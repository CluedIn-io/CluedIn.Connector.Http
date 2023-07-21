using System.Collections.Generic;
using System.Threading.Tasks;
using CluedIn.Core.Processing;

namespace CluedIn.Connector.Http.Connector
{
    public interface IHttpClient
    {
        Task<SaveResult> SendAsync(HttpConnectorJobData jobData, IDictionary<string, object> data);
    }
}
