using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Core;
using CluedIn.Core.Connectors;
using CluedIn.Core.DataStore;
using CluedIn.Core.Processing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ExecutionContext = CluedIn.Core.ExecutionContext;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpConnector : ConnectorBase
    {
        private readonly ILogger<HttpConnector> _logger;
        
        private readonly IHttpClient _client;

        public HttpConnector(IConfigurationRepository repo, ILogger<HttpConnector> logger, IHttpClient client) : base(repo)
        {
            ProviderId = HttpConstants.ProviderId;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override async Task CreateContainer(ExecutionContext executionContext, Guid providerDefinitionId, CreateContainerModel model)
        {
            await Task.FromResult(0);
        }

        public override async Task EmptyContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
        {
            await Task.FromResult(0);
        }

        public override async Task ArchiveContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
        {
            await Task.FromResult(0);
        }

        public override async Task RenameContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id, string newName)
        {
            await Task.FromResult(0);
        }

        public override async Task RemoveContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
        {
            await Task.FromResult(0);
        }

        public override Task<string> GetValidDataTypeName(ExecutionContext executionContext, Guid providerDefinitionId, string name)
        {
            // Strip non-alpha numeric characters
            var result = Regex.Replace(name, @"[^A-Za-z0-9]+", "");

            return Task.FromResult(result);
        }

        public override async Task<string> GetValidContainerName(ExecutionContext executionContext, Guid providerDefinitionId, string name)
        {
            // Strip non-alpha numeric characters
            Uri uri;
            if (Uri.TryCreate(name, UriKind.Absolute, out uri))
            {
                return await Task.FromResult(uri.AbsolutePath);
            }
            else
            {
                return await Task.FromResult(name);
            }
        }

        public override async Task<IEnumerable<IConnectorContainer>> GetContainers(ExecutionContext executionContext, Guid providerDefinitionId)
        {
            return await Task.FromResult(new List<IConnectorContainer>());
        }

        public override async Task<IEnumerable<IConnectionDataType>> GetDataTypes(ExecutionContext executionContext, Guid providerDefinitionId, string containerId)
        {
            return await Task.FromResult(new List<IConnectionDataType>());
        }

        public override async Task<bool> VerifyConnection(ExecutionContext executionContext, Guid providerDefinitionId)
        {
            return await Task.FromResult(true);
        }

        public override async Task<bool> VerifyConnection(ExecutionContext executionContext, IDictionary<string, object> config)
        {
            return await Task.FromResult(true);
        }

        public override async Task StoreData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, IDictionary<string, object> data)
        {
            try
            {
                await ActionExtensions.ExecuteWithRetry(async () =>
                {
                    var config = await base
                        .GetAuthenticationDetails(
                            executionContext, providerDefinitionId);
                    var httpClient = GetHttpClient(config);
                    var json = GetJsonFromData(data);
                    var request = GetRequest(config, json);

                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                    using (HttpResponseMessage response 
                        = httpClient.PostAsync(
                            httpClient.BaseAddress,
                            request.Content, 
                            cancellationTokenSource.Token).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                var responseJson = content.ReadAsStringAsync().Result;
                                _logger.LogInformation(
                                    $@"Sent outgoing data. Uri: {{uri}} Response: {{result}}",
                                    (string)config.Authentication[HttpConstants.KeyName.Url],
                                    response.StatusCode);
                            }
                        } else {
                            _logger.LogError(
                                $@"Failed to send outgoing custom hook to external party.
                                    Uri: {{uri}} Response: {{result}}",
                                (string)config.Authentication[HttpConstants.KeyName.Url],
                                response.StatusCode);
                        }
                    }

                },
                isTransient: ExceptionHelper.ShouldRequeue);

            }
            catch (Exception e)
            {
                var message = $"Could not store data into Container '{containerName}' for Connector {providerDefinitionId}";
                _logger.LogError(e, message);
                throw new StoreDataException(message);
            }
        }

        private static HttpClient GetHttpClient(IConnectorConnection config)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new System.Uri((string)config.Authentication[HttpConstants.KeyName.Url]);
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static HttpRequestMessage GetRequest(IConnectorConnection config, string json)
        {
            HttpRequestMessage request
                                    = new HttpRequestMessage(HttpMethod.Post, "relativeAddress");

            if (config.Authentication.ContainsKey(HttpConstants.KeyName.Authorization)
                && (string)config.Authentication[HttpConstants.KeyName.Authorization] != null)
            {
                request.Headers.Add(
                    "Authorization",
                    (string)config.Authentication[HttpConstants.KeyName.Authorization]);
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return request;
        }

        private static string GetJsonFromData(IDictionary<string, object> data)
        {
            var json = new JObject();
            foreach (var kp in data)
            {
                if (kp.Value != null)
                {
                    if (kp.Value.ToString() != string.Empty)
                        json.Add(kp.Key, kp.Value.ToString());
                }
            }
            var r = JsonUtility.Serialize(json);
            return r;
        }

        public override async Task StoreEdgeData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, string originEntityCode, IEnumerable<string> edges)
        {
            await Task.FromResult(0);
        }
    }
}
