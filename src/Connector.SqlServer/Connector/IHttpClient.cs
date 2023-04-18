using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CluedIn.Core.Connectors;

namespace CluedIn.Connector.Http.Connector
{
    public interface IHttpClient
    {
        Task SendAsync(IConnectorConnection config, Guid providerDefinitionId, string containerName, IDictionary<string, object> data);
    }
}
