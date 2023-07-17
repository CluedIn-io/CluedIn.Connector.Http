using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Connector.Http.Services;
using CluedIn.Core;
using CluedIn.Core.Connectors;
using CluedIn.Core.Data.Relational;
using CluedIn.Core.Processing;
using CluedIn.Core.Streams.Models;
using ExecutionContext = CluedIn.Core.ExecutionContext;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpConnector : ConnectorBaseV2
    {
        private readonly IHttpClient _client;
        private readonly IClock _clock;
        private readonly ICorrelationIdGenerator _correlationIdGenerator;

        public HttpConnector(IHttpClient client, IClock clock, ICorrelationIdGenerator correlationIdGenerator) : base(HttpConstants.ProviderId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _correlationIdGenerator = correlationIdGenerator ?? throw new ArgumentNullException(nameof(correlationIdGenerator));
        }

        public override async Task CreateContainer(ExecutionContext executionContext, Guid connectorProviderDefinitionId, IReadOnlyCreateContainerModelV2 model)
        {
            await Task.FromResult(0);
        }

        public override async Task EmptyContainer(ExecutionContext executionContext, IReadOnlyStreamModel streamModel)
        {
            await Task.FromResult(0);
        }

        public override async Task ArchiveContainer(ExecutionContext executionContext, IReadOnlyStreamModel streamModel)
        {
            await Task.FromResult(0);
        }

        public override async Task RenameContainer(ExecutionContext executionContext, IReadOnlyStreamModel streamModel, string oldContainerName)
        {
            await Task.FromResult(0);
        }

        public override async Task RemoveContainer(ExecutionContext executionContext, IReadOnlyStreamModel streamModel)
        {
            await Task.FromResult(0);
        }

        public override Task<string> GetValidMappingDestinationPropertyName(ExecutionContext executionContext, Guid providerDefinitionId, string name)
        {
            // Strip non-alpha numeric characters
            var result = Regex.Replace(name, @"[^A-Za-z0-9]+", "");

            return Task.FromResult(result);
        }

        public override async Task<string> GetValidContainerName(ExecutionContext executionContext, Guid providerDefinitionId, string name)
        {
            // Strip non-alpha numeric characters
            Uri uri;
            if (Uri.TryCreate(name, UriKind.Absolute, out uri))
            {
                return await Task.FromResult(uri.AbsolutePath);
            }
            else
            {
                return await Task.FromResult(name);
            }
        }

        public override async Task<IEnumerable<IConnectorContainer>> GetContainers(ExecutionContext executionContext, Guid providerDefinitionId)
        {
            return await Task.FromResult(new List<IConnectorContainer>());
        }

        public override async Task<ConnectionVerificationResult> VerifyConnection(ExecutionContext executionContext, IReadOnlyDictionary<string, object> config)
        {
            var connectionBase = new ConnectorConnectionBase
            {
                Authentication = config.ToDictionary(x => x.Key, x => x.Value)
            };

            return await VerifyConnection(connectionBase);
        }

        private async Task<ConnectionVerificationResult> VerifyConnection(IConnectorConnection config)
        {
            var url = (string)config.Authentication[HttpConstants.KeyName.Url];
            try
            {
                new Uri(url);
            }
            catch
            {
                return await Task.FromResult(new ConnectionVerificationResult(false, "Invalid URL"));
            }

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
                    using (var result = await client.SendAsync(request, cancellationTokenSource.Token))
                    {
                        return await Task.FromResult(new ConnectionVerificationResult(result.IsSuccessStatusCode, result.StatusCode.ToString()));
                    }
                }
            }
        }

        public override Task VerifyExistingContainer(ExecutionContext executionContext, IReadOnlyStreamModel streamModel)
        {
            return Task.FromResult(0);
        }

        public override async Task<SaveResult> StoreData(ExecutionContext executionContext, IReadOnlyStreamModel streamModel, IReadOnlyConnectorEntityData connectorEntityData)
        {
            var providerDefinitionId = streamModel.ConnectorProviderDefinitionId!.Value;
            var containerName = streamModel.ContainerName;

            // matching output format of previous version of the connector
            var data = connectorEntityData.Properties.ToDictionary(x => x.Name, x => x.Value);
            data.Add("Id", connectorEntityData.EntityId);

            if (connectorEntityData.PersistInfo != null)
            {
                data.Add("PersistHash", connectorEntityData.PersistInfo.PersistHash);
            }

            if (connectorEntityData.OriginEntityCode != null)
            {
                data.Add("OriginEntityCode", connectorEntityData.OriginEntityCode.ToString());
            }

            if (connectorEntityData.EntityType != null)
            {
                data.Add("EntityType", connectorEntityData.EntityType.ToString());
            }

            data.Add("Codes", connectorEntityData.EntityCodes.Select(c => c.ToString()));
            // end match previous version of the connector

            if (connectorEntityData.OutgoingEdges.SafeEnumerate().Any())
            {
                data.Add("OutgoingEdges", connectorEntityData.OutgoingEdges);
            }

            if (connectorEntityData.IncomingEdges.SafeEnumerate().Any())
            {
                data.Add("IncomingEdges", connectorEntityData.IncomingEdges);
            }

            data.Add("ChangeType", connectorEntityData.ChangeType.ToString());

            var config = await GetAuthenticationDetails(executionContext, providerDefinitionId);

            if (connectorEntityData.StreamMode == StreamMode.EventStream)
            {
                // TODO timestamp & correlationId were passed in the signature in the previous version. Check where we get these now as they do not appear to be part of ConnectorEntityData
                data = new Dictionary<string, object>
                        {
                            { "TimeStamp", _clock.Now },
                            { "VersionChangeType", connectorEntityData.ChangeType.ToString() },
                            { "CorrelationId", _correlationIdGenerator.Next() },
                            { "Data", data }
                        };
            }

            return await _client.SendAsync(config, providerDefinitionId, containerName, data);
        }

        public override Task<ConnectorLatestEntityPersistInfo> GetLatestEntityPersistInfo(ExecutionContext executionContext, IReadOnlyStreamModel streamModel, Guid entityId)
        {
            throw new NotSupportedException();
        }

        public override Task<IAsyncEnumerable<ConnectorLatestEntityPersistInfo>> GetLatestEntityPersistInfos(ExecutionContext executionContext, IReadOnlyStreamModel streamModel)
        {
            throw new NotSupportedException();
        }

        public override IReadOnlyCollection<StreamMode> GetSupportedModes()
        {
            return new List<StreamMode> { StreamMode.Sync, StreamMode.EventStream };
        }

        public virtual async Task<IConnectorConnectionV2> GetAuthenticationDetails(ExecutionContext executionContext, Guid providerDefinitionId)
        {
            return await AuthenticationDetailsHelper.GetAuthenticationDetails(executionContext, providerDefinitionId);
        }
    }
}
