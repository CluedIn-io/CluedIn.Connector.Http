using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Core;
using CluedIn.Core.Connectors;
using CluedIn.Core.Processing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpPostClient : IHttpClient
    {
        private readonly ILogger<HttpPostClient> _logger;
        public HttpPostClient(ILogger<HttpPostClient> logger)
        {
            _logger = logger;
        }

        public async Task SendAsync(IConnectorConnection config, Guid providerDefinitionId, string containerName, IDictionary<string, object> data)
        {
            try
            {
                await ActionExtensions.ExecuteWithRetry(async () =>
                {

                    using (var client = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, (string)config.Authentication[HttpConstants.KeyName.Url]))
                        {
                            if (config.Authentication.ContainsKey(HttpConstants.KeyName.Authorization))
                            {
                                if (!string.IsNullOrEmpty(config.Authentication[HttpConstants.KeyName.Authorization].ToString().Trim()))
                                {
                                    request.Headers.Add("Authorization", (string)config.Authentication[HttpConstants.KeyName.Authorization]);
                                }
                            }

                            request.Headers.Add("X-Subject-Id", containerName);
                            var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                            var cancellationTokenSource = new CancellationTokenSource();
                            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                            using (var result = await client.SendAsync(request, cancellationTokenSource.Token))
                            {
                                if (result.IsSuccessStatusCode)
                                {
                                    _logger.LogInformation("Sent outgoing data. Uri: {uri} Response: {result}", config.Authentication[HttpConstants.KeyName.Url].ToString(), result.StatusCode);
                                }
                                else
                                {
                                    _logger.LogError("Failed to send outgoing custom hook to external party. Uri: {uri} Response: {result}", config.Authentication[HttpConstants.KeyName.Url].ToString(), result.StatusCode);
                                }
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
    }
}
