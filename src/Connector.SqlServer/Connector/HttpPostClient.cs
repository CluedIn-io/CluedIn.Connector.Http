using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Core.Processing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpPostClient : IHttpClient
    {
        private readonly ILogger<HttpPostClient> _logger;
        public HttpPostClient(ILogger<HttpPostClient> logger)
        {
            _logger = logger;
        }

        public async Task<SaveResult> SendAsync(HttpConnectorJobData jobData, IDictionary<string, object> data)
        {
            try
            {
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, jobData.Url);
                if (!string.IsNullOrEmpty(jobData.Authorization?.Trim()))
                {
                    request.Headers.Add("Authorization", jobData.Authorization);
                }

                request.Headers.Add("X-Subject-Id", jobData.ContainerName);

                var json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                using var result = await client.SendAsync(request, cancellationTokenSource.Token);
                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to send outgoing custom hook to external party. Uri: {uri} Response: {result}", jobData.Url, result.StatusCode);
                    return new SaveResult(SaveResultState.ReQueue);
                }

                _logger.LogDebug("Sent outgoing data. Uri: {uri} Response: {result}", jobData.Url, result.StatusCode);
                return new SaveResult(SaveResultState.Success);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not store data into Container '{jobData.ContainerName}' for Connector {jobData.ProviderDefinitionId}");

                return new SaveResult(SaveResultState.ReQueue);
            }
        }
    }
}
