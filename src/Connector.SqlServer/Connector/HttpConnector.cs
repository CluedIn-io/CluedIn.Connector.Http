using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Core;
using CluedIn.Core.Connectors;
using CluedIn.Core.Data.Vocabularies;
using CluedIn.Core.DataStore;
using CluedIn.Core.Processing;
using Microsoft.Data.SqlClient;
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
                    var config = await base.GetAuthenticationDetails(executionContext, providerDefinitionId);

                    using (var client = new HttpClient())
                    using (var request = new HttpRequestMessage(HttpMethod.Post, (string)config.Authentication[HttpConstants.KeyName.Url]))
                    {
                        if (config.Authentication.ContainsKey(HttpConstants.KeyName.Authorization))
                        {
                            if ((string)config.Authentication[HttpConstants.KeyName.Authorization] != null)
                            {
                                request.Headers.Add("Authorization", (string)config.Authentication[HttpConstants.KeyName.Authorization]);
                            }
                        }
                        
                        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(hookDefinition.MimeType));
                        // request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CluedIn-Server", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                        request.Headers.Add("Content-Type", "application/json");
                        request.Headers.Add("X-Subject-Id", containerName);
                        request.Content = new PushStreamContent((stream, httpContent, transportContext) =>
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

                            using (var textWriter = new StreamWriter(stream))
                                JsonUtility.Serialize(json, textWriter);
                        }, MediaTypeHeaderValue.Parse("application/json"));

                        var cancellationTokenSource = new CancellationTokenSource();
                        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                        using (var result = await client.SendAsync(request, cancellationTokenSource.Token))
                        {
                            if (result.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Sent outgoing data. Uri: {uri} Response: {result}", HttpConstants.KeyName.Url, result.StatusCode);
                            }
                            else
                            {
                                _logger.LogError("Failed to send outgoing custom hook to external party. Uri: {uri} Response: {result}", HttpConstants.KeyName.Url, result.StatusCode);
                            }
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

        public override async Task StoreEdgeData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, string originEntityCode, IEnumerable<string> edges)
        {
            await Task.FromResult(0);
        }
    }
}
