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
using CluedIn.Connector.Http.Services;
using CluedIn.Core.Caching;
using CluedIn.Core.Connectors;
using CluedIn.Core.Data;
using CluedIn.Core.Data.Parts;
using CluedIn.Core.Data.Vocabularies;
using CluedIn.Core.Processing;
using CluedIn.Core.Streams.Models;

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

            container.Register(Component.For<HttpConnector>());

            var executionContext = container.Resolve<ExecutionContext>();

            var connector = container.Resolve<HttpConnector>();

            // act
            var response = await connector.VerifyConnection(executionContext,
                new Dictionary<string, object> { { HttpConstants.KeyName.Url, url } });

            // assert
            response.Should().NotBeNull();
            response.Success.Should().Be(false);
            response.ErrorMessage.Should().NotBe(null);
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

            container.Register(Component.For<HttpConnector>());

            var executionContext = container.Resolve<ExecutionContext>();

            var connector = container.Resolve<HttpConnector>();

            // act
            var response = await connector.VerifyConnection(executionContext,
                new Dictionary<string, object> { { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" } });

            // assert
            response.Should().NotBeNull();
            response.Success.Should().Be(true);

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

            var connectionMock = new Mock<IConnectorConnectionV2>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" },
                { HttpConstants.KeyName.Authorization, "authvalue" },
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            var data = new ConnectorEntityData(VersionChangeType.Added, StreamMode.Sync,
                Guid.Parse("69e26b81-bcbf-54f7-af97-be056f73bf9a"),
                new ConnectorEntityPersistInfo("1lzghdhhgqlnucj078/77q==", 1), null,
                EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"),
                "/Person",
                new[]
                {
                    new ConnectorPropertyData("Name", "Jean Luc Picard",
                        new EntityPropertyConnectorPropertyDataType(typeof(string))),
                    new ConnectorPropertyData("user.lastName", "Picard",
                        new VocabularyKeyConnectorPropertyDataType(new VocabularyKey("user.lastName")))
                },
                new IEntityCode[] { EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0") },
                null, null);

            var streamModel = new Mock<IReadOnlyStreamModel>();
            streamModel.Setup(x => x.ConnectorProviderDefinitionId).Returns(Guid.Empty);
            streamModel.Setup(x => x.ContainerName).Returns("test_container");

            // act
            await connector.StoreData(executionContext, streamModel.Object, data);

            // assert
            serverReceivedRequest.Should().NotBeNull();

            serverReceivedRequest.Should().Be($@"POST / HTTP/1.1
Host: {l.LocalEndpoint}
Authorization: authvalue
X-Subject-Id: test_container
Content-Type: application/json; charset=utf-8
Content-Length: 377

{{
  ""Name"": ""Jean Luc Picard"",
  ""user.lastName"": ""Picard"",
  ""Id"": ""69e26b81-bcbf-54f7-af97-be056f73bf9a"",
  ""PersistHash"": ""1lzghdhhgqlnucj078/77q=="",
  ""OriginEntityCode"": ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"",
  ""EntityType"": ""/Person"",
  ""Codes"": [
    ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0""
  ],
  ""ChangeType"": ""Added""
}}");
        }

        [Fact]
        internal async Task VerifyStoreEventData()
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

            var mockClock = new Mock<IClock>();
            mockClock.Setup(x => x.Now).Returns(new DateTimeOffset(2023, 4, 25, 19, 18, 23, TimeSpan.FromHours(10)));
            container.Register(Component.For<IClock>().Instance(mockClock.Object));

            var mockIdGenerator = new Mock<ICorrelationIdGenerator>();
            mockIdGenerator.Setup(x => x.Next()).Returns("corroid");
            container.Register(Component.For<ICorrelationIdGenerator>().Instance(mockIdGenerator.Object));

            var executionContext = container.Resolve<ExecutionContext>();

            var connectorMock = new Mock<HttpConnector>(MockBehavior.Default,
                typeof(HttpConnector).GetConstructors().First().GetParameters()
                    .Select(p => container.Resolve(p.ParameterType)).ToArray());

            var connectionMock = new Mock<IConnectorConnectionV2>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" },
                { HttpConstants.KeyName.Authorization, "authvalue" },
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            var data = new ConnectorEntityData(VersionChangeType.Added, StreamMode.EventStream,
                Guid.Parse("69e26b81-bcbf-54f7-af97-be056f73bf9a"),
                new ConnectorEntityPersistInfo("1lzghdhhgqlnucj078/77q==", 1), null,
                EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"),
                "/Person",
                new[]
                {
                    new ConnectorPropertyData("Name", "Jean Luc Picard",
                        new EntityPropertyConnectorPropertyDataType(typeof(string))),
                    new ConnectorPropertyData("user.lastName", "Picard",
                        new VocabularyKeyConnectorPropertyDataType(new VocabularyKey("user.lastName")))
                },
                new IEntityCode[] { EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0") },
                null, null);

            var streamModel = new Mock<IReadOnlyStreamModel>();
            streamModel.Setup(x => x.ConnectorProviderDefinitionId).Returns(Guid.Empty);
            streamModel.Setup(x => x.ContainerName).Returns("test_container");

            // act
            await connector.StoreData(executionContext, streamModel.Object, data);

            // assert
            serverReceivedRequest.Should().NotBeNull();

            serverReceivedRequest.Should().Be($@"POST / HTTP/1.1
Host: {l.LocalEndpoint}
Authorization: authvalue
X-Subject-Id: test_container
Content-Type: application/json; charset=utf-8
Content-Length: 548

{{
  ""TimeStamp"": ""2023-04-25T19:18:23+10:00"",
  ""Epoch"": 1682414303,
  ""VersionChangeType"": ""Added"",
  ""CorrelationId"": ""corroid"",
  ""Data"": {{
    ""Name"": ""Jean Luc Picard"",
    ""user.lastName"": ""Picard"",
    ""Id"": ""69e26b81-bcbf-54f7-af97-be056f73bf9a"",
    ""PersistHash"": ""1lzghdhhgqlnucj078/77q=="",
    ""OriginEntityCode"": ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"",
    ""EntityType"": ""/Person"",
    ""Codes"": [
      ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0""
    ],
    ""ChangeType"": ""Added""
  }}
}}");
        }

        [Fact]
        internal async Task VerifyStoreDataWithEdges()
        {
            // arrange
            string serverReceivedRequest = null;

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();

            l.BeginAcceptTcpClient(result =>
            {
                var client = l.EndAcceptTcpClient(result);
                var clientStream = client.GetStream();

                var buffer = new byte[4 * 1024];

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

            var connectionMock = new Mock<IConnectorConnectionV2>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" },
                { HttpConstants.KeyName.Authorization, "authvalue" },
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            var data = new ConnectorEntityData(VersionChangeType.Added, StreamMode.Sync,
                Guid.Parse("69e26b81-bcbf-54f7-af97-be056f73bf9a"),
                new ConnectorEntityPersistInfo("1lzghdhhgqlnucj078/77q==", 1), null,
                EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"),
                "/Person",
                new[]
                {
                    new ConnectorPropertyData("Name", "Jean Luc Picard",
                        new EntityPropertyConnectorPropertyDataType(typeof(string))),
                    new ConnectorPropertyData("user.lastName", "Picard",
                        new VocabularyKeyConnectorPropertyDataType(new VocabularyKey("user.lastName")))
                },
                new IEntityCode[] { EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0") },
                new[]
                {
                    new EntityEdge(
                        new EntityReference(
                            EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0")),
                        new EntityReference(EntityCode.FromKey("/EntityA#Somewhere:1234")), "/EntityA")
                },
                new[]
                {
                    new EntityEdge(new EntityReference(EntityCode.FromKey("/EntityB#Somewhere:5678")),
                        new EntityReference(
                            EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0")),
                        "/EntityB")
                });

            var streamModel = new Mock<IReadOnlyStreamModel>();
            streamModel.Setup(x => x.ConnectorProviderDefinitionId).Returns(Guid.Empty);
            streamModel.Setup(x => x.ContainerName).Returns("test_container");

            // act
            await connector.StoreData(executionContext, streamModel.Object, data);

            // assert
            serverReceivedRequest.Should().NotBeNull();

            serverReceivedRequest.Should().Be($@"POST / HTTP/1.1
Host: {l.LocalEndpoint}
Authorization: authvalue
X-Subject-Id: test_container
Content-Type: application/json; charset=utf-8
Content-Length: 3527

{{
  ""Name"": ""Jean Luc Picard"",
  ""user.lastName"": ""Picard"",
  ""Id"": ""69e26b81-bcbf-54f7-af97-be056f73bf9a"",
  ""PersistHash"": ""1lzghdhhgqlnucj078/77q=="",
  ""OriginEntityCode"": ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"",
  ""EntityType"": ""/Person"",
  ""Codes"": [
    ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0""
  ],
  ""OutgoingEdges"": [
    {{
      ""FromReference"": {{
        ""Code"": {{
          ""Origin"": {{
            ""Code"": ""Somewhere"",
            ""Id"": null
          }},
          ""Value"": ""5678"",
          ""Key"": ""/EntityB#Somewhere:5678"",
          ""Type"": {{
            ""IsEntityContainer"": false,
            ""Root"": null,
            ""Code"": ""/EntityB""
          }}
        }},
        ""Type"": {{
          ""IsEntityContainer"": false,
          ""Root"": null,
          ""Code"": ""/EntityB""
        }},
        ""Name"": null,
        ""Properties"": null,
        ""PropertyCount"": null,
        ""EntityId"": null,
        ""IsEmpty"": false
      }},
      ""ToReference"": {{
        ""Code"": {{
          ""Origin"": {{
            ""Code"": ""Acceptance"",
            ""Id"": null
          }},
          ""Value"": ""7c5591cf-861a-4642-861d-3b02485854a0"",
          ""Key"": ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"",
          ""Type"": {{
            ""IsEntityContainer"": false,
            ""Root"": null,
            ""Code"": ""/Person""
          }}
        }},
        ""Type"": {{
          ""IsEntityContainer"": false,
          ""Root"": null,
          ""Code"": ""/Person""
        }},
        ""Name"": null,
        ""Properties"": null,
        ""PropertyCount"": null,
        ""EntityId"": null,
        ""IsEmpty"": false
      }},
      ""EdgeType"": {{
        ""Root"": null,
        ""Code"": ""/EntityB""
      }},
      ""HasProperties"": false,
      ""Properties"": {{}},
      ""CreationOptions"": 0,
      ""Weight"": null,
      ""Version"": 0
    }}
  ],
  ""IncomingEdges"": [
    {{
      ""FromReference"": {{
        ""Code"": {{
          ""Origin"": {{
            ""Code"": ""Acceptance"",
            ""Id"": null
          }},
          ""Value"": ""7c5591cf-861a-4642-861d-3b02485854a0"",
          ""Key"": ""/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"",
          ""Type"": {{
            ""IsEntityContainer"": false,
            ""Root"": null,
            ""Code"": ""/Person""
          }}
        }},
        ""Type"": {{
          ""IsEntityContainer"": false,
          ""Root"": null,
          ""Code"": ""/Person""
        }},
        ""Name"": null,
        ""Properties"": null,
        ""PropertyCount"": null,
        ""EntityId"": null,
        ""IsEmpty"": false
      }},
      ""ToReference"": {{
        ""Code"": {{
          ""Origin"": {{
            ""Code"": ""Somewhere"",
            ""Id"": null
          }},
          ""Value"": ""1234"",
          ""Key"": ""/EntityA#Somewhere:1234"",
          ""Type"": {{
            ""IsEntityContainer"": false,
            ""Root"": null,
            ""Code"": ""/EntityA""
          }}
        }},
        ""Type"": {{
          ""IsEntityContainer"": false,
          ""Root"": null,
          ""Code"": ""/EntityA""
        }},
        ""Name"": null,
        ""Properties"": null,
        ""PropertyCount"": null,
        ""EntityId"": null,
        ""IsEmpty"": false
      }},
      ""EdgeType"": {{
        ""Root"": null,
        ""Code"": ""/EntityA""
      }},
      ""HasProperties"": false,
      ""Properties"": {{}},
      ""CreationOptions"": 0,
      ""Weight"": null,
      ""Version"": 0
    }}
  ],
  ""ChangeType"": ""Added""
}}");
        }

        [Fact]
        internal async Task VerifyStoreDataReturnReQueueForHttpError()
        {
            // arrange
            string serverReceivedRequest = null;

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();

            l.BeginAcceptTcpClient(result =>
            {
                var client = l.EndAcceptTcpClient(result);
                var clientStream = client.GetStream();

                var buffer = new byte[4 * 1024];

                var read = clientStream.Read(buffer, 0, buffer.Length);

                serverReceivedRequest = Encoding.UTF8.GetString(buffer, 0, read);

                clientStream.Write(Encoding.UTF8.GetBytes(@"HTTP/1.1 500 Internal Server Error
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

            var connectionMock = new Mock<IConnectorConnectionV2>();
            connectionMock.Setup(x => x.Authentication).Returns(new Dictionary<string, object>
            {
                { HttpConstants.KeyName.Url, $"http://{l.LocalEndpoint}/" },
                { HttpConstants.KeyName.Authorization, "authvalue" },
            });

            connectorMock.CallBase = true;
            connectorMock.Setup(x => x.GetAuthenticationDetails(executionContext, Guid.Empty))
                .ReturnsAsync(connectionMock.Object);

            var connector = connectorMock.Object;

            var data = new ConnectorEntityData(VersionChangeType.Added, StreamMode.Sync,
                Guid.Parse("69e26b81-bcbf-54f7-af97-be056f73bf9a"),
                new ConnectorEntityPersistInfo("1lzghdhhgqlnucj078/77q==", 1), null,
                EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0"),
                "/Person",
                new[]
                {
                    new ConnectorPropertyData("Name", "Jean Luc Picard",
                        new EntityPropertyConnectorPropertyDataType(typeof(string))),
                    new ConnectorPropertyData("user.lastName", "Picard",
                        new VocabularyKeyConnectorPropertyDataType(new VocabularyKey("user.lastName")))
                },
                new IEntityCode[] { EntityCode.FromKey("/Person#Acceptance:7c5591cf-861a-4642-861d-3b02485854a0") },
                null, null);

            var streamModel = new Mock<IReadOnlyStreamModel>();
            streamModel.Setup(x => x.ConnectorProviderDefinitionId).Returns(Guid.Empty);
            streamModel.Setup(x => x.ContainerName).Returns("test_container");

            // act
            var saveResult = await connector.StoreData(executionContext, streamModel.Object, data);

            // assert
            serverReceivedRequest.Should().NotBeNull();
            saveResult.State.Should().Be(SaveResultState.ReQueue);
        }
    }
}
