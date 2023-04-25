using CluedIn.Connector.Http.Connector;
using CluedIn.Core.DataStore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CluedIn.Connector.Http.Unit.Tests
{
    public class HttpConnectorTestsBase
    {
        protected readonly HttpConnector Sut;
        protected readonly Mock<IConfigurationRepository> Repo = new Mock<IConfigurationRepository>();
        protected readonly Mock<ILogger<HttpConnector>> Logger = new Mock<ILogger<HttpConnector>>();
        protected readonly Mock<IHttpClient> Client = new Mock<IHttpClient>();
        protected readonly TestContext Context = new TestContext();

        public HttpConnectorTestsBase()
        {
            //Sut = new HttpConnector(Repo.Object, Logger.Object, Client.Object);
        }
    }
}
