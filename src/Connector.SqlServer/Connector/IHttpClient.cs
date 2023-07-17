using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CluedIn.Core.Connectors;
using CluedIn.Core.Processing;

namespace CluedIn.Connector.Http.Connector
{
    public interface IHttpClient
    {
        Task<SaveResult> SendAsync(IConnectorConnectionV2 config, Guid providerDefinitionId, string containerName, IDictionary<string, object> data);
    }
}
