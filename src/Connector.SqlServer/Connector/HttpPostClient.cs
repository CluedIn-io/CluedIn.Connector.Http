using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Connectors.Batching;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpPostClient : IHttpClient
    {
        private readonly ILogger<HttpPostClient> _logger;
        private readonly JsonSerializerSettings _serializerSettings;

        public HttpPostClient(ILogger<HttpPostClient> logger)
        {
            _logger = logger;
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }

        public async Task SendAsync(HttpConnectorJobData jobData, IDictionary<string, object>[] data)
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("X-Subject-Id", jobData.ContainerName)
            };
            if (!string.IsNullOrEmpty(jobData.Authorization?.Trim()))
            {
                headers.Add(new KeyValuePair<string, string>("Authorization", jobData.Authorization));
            }

            try
            {
                using var client = new HttpClient();
                if (jobData.BatchingSupported)
                {
                    await SendRequest(client, jobData, headers, data);
                }
                else
                {
                    foreach (var item in data)
                    {
                        await SendRequest(client, jobData, headers, item);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not store data into Container '{jobData.ContainerName}' for Connector {jobData.ProviderDefinitionId}");
                throw;
            }
        }

        private async Task SendRequest(HttpClient client, HttpConnectorJobData jobData, List<KeyValuePair<string, string>> headers, object data)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, jobData.Url);
            foreach(var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var json = JsonConvert.SerializeObject(data, _serializerSettings);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            using var result = await client.SendAsync(request, cancellationTokenSource.Token);
            if (!result.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to send outgoing custom hook to external party. Uri: {jobData.Url} Response: {result.StatusCode}");
            }

            _logger.LogDebug("Sent outgoing data. Uri: {uri} Response: {result}", jobData.Url, result.StatusCode);
        }
    }
}
