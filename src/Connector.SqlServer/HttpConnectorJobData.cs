using System;
using System.Collections.Generic;
using CluedIn.Core.Crawling;

namespace CluedIn.Connector.Http
{
    public class HttpConnectorJobData : CrawlJobData
    {
        public HttpConnectorJobData(IDictionary<string, object> configuration, string containerName = null, Guid? providerDefinitionId = null)
        {
            Configurations = configuration;
            ContainerName = containerName;
            ProviderDefinitionId = providerDefinitionId;

            Authorization = Configurations.TryGetValue(HttpConstants.KeyName.Authorization, out var authConfig) ? authConfig.ToString() : null;
            BatchingSupported = Configurations.TryGetValue(HttpConstants.BatchingSupported, out var batchingConfig) ? (bool) batchingConfig : false;
            Url = Configurations.TryGetValue(HttpConstants.KeyName.Url, out var urlConfig) ? urlConfig.ToString() : null;
        }

        public IDictionary<string, object> ToDictionary()
        {
            return Configurations;
        }

        public IDictionary<string, object> Configurations { get; }
        public string Authorization { get; }
        public bool BatchingSupported { get; }
        public string ContainerName { get; }
        public Guid? ProviderDefinitionId { get; }
        public string Url { get; }
    }
}
