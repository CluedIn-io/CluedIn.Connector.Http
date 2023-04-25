using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CluedIn.Connector.Http.Connector;
using CluedIn.Core;
using FluentAssertions;
using Moq;
using Castle.MicroKernel.Resolvers;
using CluedIn.Core.Caching;
using CluedIn.Core.Connectors;

namespace CluedIn.Connector.Http.Unit.Tests
{
    public class HttpConnectorTests
    {
        [Theory]
        [InlineData("invalid_url")]
        public async Task VerifyConnectionValidationFailsForInvalidUrl(string url)
        {
            // arrange
            var container = new WindsorContainer();
            container.Register(Component.For<IWindsorContainer>().Instance(container));
            container.Register(Component.For<IApplicationCache>().ImplementedBy<InMemoryApplicationCache>());
            container.Register(Component.For<ILazyComponentLoader>().ImplementedBy<AutoMockingLazyComponentLoader>());

            var executionContext = container.Resolve<ExecutionContext>();

            var connectorMock = new Mock<HttpConnector>(MockBehavior.Default,
                typeof(HttpConnector).GetConstructors().First().GetParameters()
                    .Select(p => container.Resolve(p.ParameterType)).ToArray());

            var connectionMock = new Mock<IConnectorConnection>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, url }
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            // act
            var response = await connector.VerifyConnection(executionContext, Guid.Empty);

            // assert
            response.Should().Be(false);
        }

        [Fact]
        public async Task VerifyConnectionValidationInvokesUrlAndReturnsTrue()
        {
            // arrange
            string serverReceivedRequest = null;

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();

            l.BeginAcceptTcpClient(result =>
            {
                var client = l.EndAcceptTcpClient(result);
                var clientStream = client.GetStream();

                var buffer = new byte[1024];

                var read = clientStream.Read(buffer, 0, buffer.Length);

                serverReceivedRequest = Encoding.UTF8.GetString(buffer, 0, read);

                clientStream.Write(Encoding.UTF8.GetBytes(@"HTTP/1.1 200 OK
Content-Length: 0

"));

            }, null);

            var container = new WindsorContainer();
            container.Register(Component.For<IWindsorContainer>().Instance(container));
            container.Register(Component.For<IApplicationCache>().ImplementedBy<InMemoryApplicationCache>());
            container.Register(Component.For<ILazyComponentLoader>().ImplementedBy<AutoMockingLazyComponentLoader>());

            var executionContext = container.Resolve<ExecutionContext>();

            var connectorMock = new Mock<HttpConnector>(MockBehavior.Default,
                typeof(HttpConnector).GetConstructors().First().GetParameters()
                    .Select(p => container.Resolve(p.ParameterType)).ToArray());

            var connectionMock = new Mock<IConnectorConnection>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" }
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            // act
            var response = await connector.VerifyConnection(executionContext, Guid.Empty);

            // assert
            response.Should().Be(true);

            serverReceivedRequest.Should().NotBeNull();
            serverReceivedRequest.Should().MatchRegex("^HEAD / HTTP/1\\.[01]");
        }

        [Fact]
        internal async Task VerifyStoreData()
        {
            // arrange
            string serverReceivedRequest = null;

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();

            l.BeginAcceptTcpClient(result =>
            {
                var client = l.EndAcceptTcpClient(result);
                var clientStream = client.GetStream();

                var buffer = new byte[1024];

                var read = clientStream.Read(buffer, 0, buffer.Length);

                serverReceivedRequest = Encoding.UTF8.GetString(buffer, 0, read);

                clientStream.Write(Encoding.UTF8.GetBytes(@"HTTP/1.1 200 OK
Content-Length: 0

"));

            }, null);

            var container = new WindsorContainer();
            container.Register(Component.For<IWindsorContainer>().Instance(container));
            container.Register(Component.For<IApplicationCache>().ImplementedBy<InMemoryApplicationCache>());
            container.Register(Component.For<IHttpClient>().ImplementedBy<HttpPostClient>());
            container.Register(Component.For<ILazyComponentLoader>().ImplementedBy<AutoMockingLazyComponentLoader>());

            var executionContext = container.Resolve<ExecutionContext>();

            var connectorMock = new Mock<HttpConnector>(MockBehavior.Default,
                typeof(HttpConnector).GetConstructors().First().GetParameters()
                    .Select(p => container.Resolve(p.ParameterType)).ToArray());

            var connectionMock = new Mock<IConnectorConnection>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" }
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            var data = new Dictionary<string, object>();
            data.Add("test", "value");

            // act
            await connector.StoreData(executionContext, Guid.Empty, "test_container", data);

            // assert
            serverReceivedRequest.Should().NotBeNull();

            serverReceivedRequest.Should().Be($@"POST / HTTP/1.1
Host: {l.LocalEndpoint}
X-Subject-Id: test_container
Content-Type: application/json; charset=utf-8
Content-Length: 23

{{
  ""test"": ""value""
}}");
        }
    }
}
