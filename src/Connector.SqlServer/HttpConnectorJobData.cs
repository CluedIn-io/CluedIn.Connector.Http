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
                { HttpConstants.KeyName.Url, Url },
                { nameof(mykeyname), mykeyname },
                { nameof(country), country },
                { nameof(state), state },
                { nameof(city), city },
                { nameof(outputFormat2), outputFormat2 },
                { nameof(delimiter2), delimiter2 },
                { nameof(subscription), subscription },
                { nameof(resourceGroup), resourceGroup },
                { nameof(resource), resource },
            };
        }

        public string Authorization { get; set; }

        public string Url { get; set; }

        public string mykeyname { get; set; }

        public string country { get; set; }

        public string state { get; set; }

        public string city { get; set; }

        public string outputFormat2 { get; set; }

        public string delimiter2 { get; set; }

        public string subscription { get; set; }

        public string resourceGroup { get; set; }

        public string resource { get; set; }
    }
}
