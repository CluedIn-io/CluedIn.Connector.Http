using System.Collections.Generic;
using System.Threading.Tasks;

namespace CluedIn.Connector.Http.Connector
{
    public interface IHttpClient
    {
        Task SendAsync(HttpConnectorJobData jobData, IDictionary<string, object>[] data);
    }
}
