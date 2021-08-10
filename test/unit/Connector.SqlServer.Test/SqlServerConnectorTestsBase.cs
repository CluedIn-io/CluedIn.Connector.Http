using CluedIn.Connector.Http.Connector;
using CluedIn.Core.DataStore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CluedIn.Connector.Http.Unit.Tests
{
    public class SqlServerConnectorTestsBase
    {
        protected readonly SqlServerConnector Sut;
        protected readonly Mock<IConfigurationRepository> Repo = new Mock<IConfigurationRepository>();
        protected readonly Mock<ILogger<SqlServerConnector>> Logger = new Mock<ILogger<SqlServerConnector>>();
        protected readonly Mock<ISqlClient> Client = new Mock<ISqlClient>();
        protected readonly TestContext Context = new TestContext();

        public SqlServerConnectorTestsBase()
        {
            Sut = new SqlServerConnector(Repo.Object, Logger.Object, Client.Object);
        }
    }
}
