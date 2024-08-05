using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using CluedIn.Connector.Http.Services;
using CluedIn.Core.Providers.ExtendedConfiguration;

namespace CluedIn.Connector.Http
{
    public class InstallComponents : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IClock>()
                    .ImplementedBy<Clock>()
                    .OnlyNewServices());
            container.Register(
                Component.For<ICorrelationIdGenerator>()
                    .ImplementedBy<CorrelationIdGenerator>()
                    .OnlyNewServices());
            container.Register(
                Component.For<IExtendedConfigurationProvider>()
                    .ImplementedBy<MyExtendedConfigurationProvider>()
                    .LifestyleSingleton());
            container.Register(
                Component.For<IExtendedConfigurationProvider>()
                    .ImplementedBy<MyAzureProvider>()
                    .LifestyleSingleton());
        }
    }
}
