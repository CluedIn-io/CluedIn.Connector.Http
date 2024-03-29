using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core;
using Castle.MicroKernel.Registration;
using CluedIn.Connector.Http.Connector;
using CluedIn.Core;
using CluedIn.Core.Accounts;
using CluedIn.Core.Data.Relational;
using CluedIn.Core.DataStore.Entities;
using CluedIn.Core.Providers;
using CluedIn.Core.Server;
using CluedIn.Core.Streams;
using CluedIn.Core.Streams.Models;
using ComponentHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CluedIn.Connector.Http
{
    [Component(HttpConstants.ProviderName, "Providers", ComponentType.Service, ServerComponents.ProviderWebApi, Components.Server, Components.DataStores, Isolation = ComponentIsolation.NotIsolated)]
    public sealed class HttpConnectorComponent : ServiceApplicationComponent<IServer>
    {
        /**********************************************************************************************************
         * CONSTRUCTOR
         **********************************************************************************************************/

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnectorComponent" /> class.
        /// </summary>
        /// <param name="componentInfo">The component information.</param>
        public HttpConnectorComponent(ComponentInfo componentInfo) : base(componentInfo)
        {
            // Dev. Note: Potential for compiler warning here ... CA2214: Do not call overridable methods in constructors
            //   this class has been sealed to prevent the CA2214 waring being raised by the compiler
            Container.Register(Component.For<HttpConnectorComponent>().Instance(this));

            //Container.Register(Component.For<ISqlClient>().ImplementedBy<SqlClient>().OnlyNewServices());
        }

        /**********************************************************************************************************
         * METHODS
         **********************************************************************************************************/

        /// <summary>Starts this instance.</summary>
        public override void Start()
        {

            Container.Install(new InstallComponents());

            var asm = Assembly.GetExecutingAssembly();
            Container.Register(Types.FromAssembly(asm).BasedOn<IProvider>().WithServiceFromInterface().If(t => !t.IsAbstract).LifestyleSingleton());
            Container.Register(Types.FromAssembly(asm).BasedOn<IEntityActionBuilder>().WithServiceFromInterface().If(t => !t.IsAbstract).LifestyleSingleton());

            Container.Register(Component.For<IHttpClient>().ImplementedBy<HttpPostClient>().OnlyNewServices());

            #region Set existing streams to EventMode
            Task.Run(async () =>
            {
                try
                {
                    var upgradeSettingKey = $"HTTP Connector Mode Migration";

                    var dbContext = new CluedInEntities(Container.Resolve<DbContextOptions<CluedInEntities>>());
                    var modeMigrationSetting = dbContext.Settings.FirstOrDefault(s =>
                        s.OrganizationId == Guid.Empty && s.Key == upgradeSettingKey);
                    if (modeMigrationSetting != null)
                    {
                        return;
                    }

                    var startedAt = DateTime.Now;

                    IStreamRepository streamRepository = null;
                    while (streamRepository == null)
                    {
                        if (DateTime.Now.Subtract(startedAt).TotalMinutes > 10)
                        {
                            Log.LogWarning($"Timeout resolving {nameof(IStreamRepository)}");
                            return;
                        }

                        try
                        {
                            streamRepository = Container.Resolve<IStreamRepository>();
                        }
                        catch
                        {
                            await Task.Delay(1000);
                        }
                    }

                    var streams = streamRepository.GetAllStreams().ToList();

                    var organizationIds = streams.Select(s => s.OrganizationId).Distinct().ToArray();

                    foreach (var orgId in organizationIds)
                    {
                        var org = new Organization(ApplicationContext, orgId);

                        foreach (var provider in org.Providers.AllProviderDefinitions.Where(x =>
                                     x.ProviderId == HttpConstants.ProviderId))
                        {
                            foreach (var stream in streams.Where(s => s.ConnectorProviderDefinitionId == provider.Id))
                            {
                                if (stream.Mode != StreamMode.EventStream)
                                {
                                    var executionContext = ApplicationContext.CreateExecutionContext(orgId);

                                    var model = new SetupConnectorModel
                                    {
                                        ConnectorProviderDefinitionId = provider.Id,
                                        Mode = StreamMode.EventStream,
                                        ContainerName = stream.ContainerName,
                                        DataTypes =
                                            (await streamRepository.GetStreamMappings(stream.Id))
                                            .Select(x => new DataTypeEntry
                                            {
                                                Key = x.SourceDataType,
                                                Type = x.SourceObjectType
                                            }).ToList(),
                                        ExistingContainerAction = ExistingContainerActionEnum.Archive,
                                        ExportIncomingEdges = stream.ExportIncomingEdges,
                                        ExportOutgoingEdges = stream.ExportOutgoingEdges,
                                        OldContainerName = stream.ContainerName,
                                    };

                                    Log.LogInformation($"Setting {nameof(StreamMode.EventStream)} for stream '{stream.Name}' ({stream.Id})");

                                    await streamRepository.SetupConnector(stream.Id, model, executionContext);
                                }
                            }
                        }
                    }

                    dbContext.Settings.Add(new Setting
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = Guid.Empty,
                        UserId = Guid.Empty,
                        Key = upgradeSettingKey,
                        Data = "Complete",
                    });
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, $"{ComponentName}: Upgrade error");
                }
            });
            #endregion

            this.Log.LogInformation($"{ComponentName} Registered");
            State = ServiceState.Started;
        }

        /// <summary>Stops this instance.</summary>
        public override void Stop()
        {
            if (State == ServiceState.Stopped)
                return;

            State = ServiceState.Stopped;
        }

        public string ComponentName => "Http Connector";
    }
}
