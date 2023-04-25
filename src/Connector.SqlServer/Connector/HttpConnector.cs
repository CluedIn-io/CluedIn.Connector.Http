using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CluedIn.Core.Caching;
using CluedIn.Core.Connectors;
//using CluedIn.Core.Data.Parts;
using CluedIn.Core.DataStore;
using CluedIn.Core.Processing;
using CluedIn.Core.Streams.Models;
using Newtonsoft.Json;
using ExecutionContext = CluedIn.Core.ExecutionContext;

namespace CluedIn.Connector.Http.Connector
{
    public class HttpConnector : ConnectorBaseV2
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IHttpClient _client;
        //private StreamMode StreamMode { get; set; } = StreamMode.Sync;

        public HttpConnector(IConfigurationRepository configurationRepository, IHttpClient client) : base(HttpConstants.ProviderId)
        {
            _configurationRepository = configurationRepository;
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override async Task CreateContainer(ExecutionContext executionContext, Guid providerDefinitionId, CreateContainerModelV2 model)
        {
            await Task.FromResult(0);
        }

        public override async Task EmptyContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
        {
            await Task.FromResult(0);
        }

        public override async Task ArchiveContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
        {
            await Task.FromResult(0);
        }

        public override async Task RenameContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id, string newName)
        {
            await Task.FromResult(0);
        }

        public override async Task RemoveContainer(ExecutionContext executionContext, Guid providerDefinitionId, string id)
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

        public override async Task<ConnectionVerificationResult> VerifyConnection(ExecutionContext executionContext, IDictionary<string, object> config)
        {
            var connectionBase = new ConnectorConnectionBase
            {
                Authentication = config
            };

            return await VerifyConnection(connectionBase);
        }

        private async Task<ConnectionVerificationResult> VerifyConnection(IConnectorConnection config)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, (string)config.Authentication[HttpConstants.KeyName.Url]))
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
                    using (var result = await client.SendAsync(request, cancellationTokenSource.Token))
                    {
                        return await Task.FromResult(new ConnectionVerificationResult(result.IsSuccessStatusCode));
                    }
                }
            }
        }

        public override Task VerifyExistingContainer(ExecutionContext executionContext, StreamModel stream)
        {
            return Task.FromResult(0);
        }

        public override async Task<SaveResult> StoreData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, ConnectorEntityData connectorEntityData)
        {
            // matching output format of previous version of the connector
            var data = connectorEntityData.Properties.ToDictionary(x => GetValidMappingDestinationPropertyName(executionContext, providerDefinitionId, x.Name).Result, x => x.Value);
            data.Add("Id", connectorEntityData.EntityId);
            data.Add("Codes",
                new Dictionary<string, object>
                {
                    {
                        "$type",
                        "System.Collections.Generic.List`1[[System.Object, System.Private.CoreLib]], System.Private.CoreLib"
                    },
                    { "$values", connectorEntityData.EntityCodes.Select(c => c.ToString()) }
                });
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
            // end match previous version of the connector

            var jsonSerializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.None };

            data.Add("OutgoingEdges", connectorEntityData.OutgoingEdges);
            data.Add("IncomingEdges", connectorEntityData.IncomingEdges);

            var config = await GetAuthenticationDetails(executionContext, providerDefinitionId);

            if (connectorEntityData.StreamMode == StreamMode.EventStream)
            {
                // TODO timestamp & correlationId were passed in the signature in the previous version. Check where we get these now as they do not appear to be part of ConnectorEntityData
                data = new Dictionary<string, object>
                        {
                            { "TimeStamp", DateTime.Now },
                            { "VersionChangeType", connectorEntityData.ChangeType.ToString() },
                            { "CorrelationId", Guid.NewGuid() },
                            { "Data", data }
                        };
            }

            return await _client.SendAsync(config, providerDefinitionId, containerName, data);
        }

        public override Task<ConnectorLatestEntityPersistInfo> GetLatestEntityPersistInfo(ExecutionContext executionContext, Guid providerDefinitionId, string containerName,
            Guid entityId)
        {
            throw new NotSupportedException();
        }

        public override Task<IAsyncEnumerable<ConnectorLatestEntityPersistInfo>> GetLatestEntityPersistInfos(ExecutionContext executionContext, Guid providerDefinitionId, string containerName)
        {
            throw new NotSupportedException();
        }

        //public override async Task StoreEdgeData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, string originEntityCode, IEnumerable<string> edges)
        //{
        //    var config = await GetAuthenticationDetails(executionContext, providerDefinitionId);

        //    var data = new Dictionary<string, object>
        //    {
        //        { "OriginEntityCode", originEntityCode },
        //        { "Edges", edges }
        //    };

        //    await _client.SendAsync(config, providerDefinitionId, containerName, data);
        //}

        public override IReadOnlyCollection<StreamMode> GetSupportedModes()
        {
            return new List<StreamMode> { StreamMode.Sync, StreamMode.EventStream };
        }

        //public void SetMode(StreamMode mode)
        //{
        //    StreamMode = mode;
        //}

        //public Task<string> GetCorrelationId()
        //{
        //    return Task.FromResult(Guid.NewGuid().ToString());
        //}

        //public async Task StoreEdgeData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, string originEntityCode, string correlationId, DateTimeOffset timestamp, VersionChangeType changeType, IEnumerable<string> edges)
        //{
        //    if (StreamMode == StreamMode.EventStream)
        //    {
        //        var config = await base.GetAuthenticationDetails(executionContext, providerDefinitionId);

        //        var dataWrapper = new Dictionary<string, object>
        //        {
        //            { "TimeStamp", timestamp },
        //            { "VersionChangeType", changeType.ToString() },
        //            { "CorrelationId", correlationId },
        //            { "OriginEntityCode", originEntityCode },
        //            { "Edges", edges },
        //        };

        //        await _client.SendAsync(config, providerDefinitionId, containerName, dataWrapper);
        //    }
        //    else
        //    {
        //        await StoreEdgeData(executionContext, providerDefinitionId, containerName, originEntityCode, edges);
        //    }
        //}

        //public async Task StoreData(ExecutionContext executionContext, Guid providerDefinitionId, string containerName, string correlationId, DateTimeOffset timestamp, VersionChangeType changeType, IDictionary<string, object> data)
        //{
        //    if (StreamMode == StreamMode.EventStream)
        //    {
        //        var config = await base.GetAuthenticationDetails(executionContext, providerDefinitionId);

        //        var dataWrapper = new Dictionary<string, object>
        //        {
        //            { "TimeStamp", timestamp },
        //            { "VersionChangeType", changeType.ToString() },
        //            { "CorrelationId", correlationId },
        //            { "Data", data }
        //        };

        //        await _client.SendAsync(config, providerDefinitionId, containerName, dataWrapper);
        //    }
        //    else
        //    {
        //        await StoreData(executionContext, providerDefinitionId, containerName, data);
        //    }
        //}

        public virtual Task<IConnectorConnection> GetAuthenticationDetails(ExecutionContext executionContext, Guid providerDefinitionId)
        {
            var key = $"AuthenticationDetails_{providerDefinitionId}";
            ICachePolicy GetPolicy(ICachePolicy cachePolicy) => new CachePolicy { SlidingExpiration = new TimeSpan(0, 0, 1, 0) };

            var result = executionContext.ApplicationContext.System.Cache.GetItem(key, () =>
            {
                var dictionary = _configurationRepository.GetConfigurationById(executionContext, providerDefinitionId);

                return new ConnectorConnectionBase { Authentication = dictionary };
            }, cachePolicy: GetPolicy);

            return Task.FromResult(result as IConnectorConnection);
        }
    }
}
