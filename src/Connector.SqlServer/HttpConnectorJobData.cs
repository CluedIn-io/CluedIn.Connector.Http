using System.Collections.Generic;
using CluedIn.Core.Crawling;

namespace CluedIn.Connector.Http
{
    public class HttpConnectorJobData : CrawlJobData
    {
        public HttpConnectorJobData(IDictionary<string, object> configuration)
        {
            if (configuration == null)
            {
                return;
            }

            Authorization = GetValue<string>(configuration, HttpConstants.KeyName.Authorization);
            Url = GetValue<string>(configuration, HttpConstants.KeyName.Url);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object> {
                { HttpConstants.KeyName.Authorization, Authorization },
                { HttpConstants.KeyName.Url, Url }
            };
        }

        public string Authorization { get; set; }

        public string Url { get; set; }
    }
}
