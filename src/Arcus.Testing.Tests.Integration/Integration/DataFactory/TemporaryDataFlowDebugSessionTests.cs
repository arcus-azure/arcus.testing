using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    public class TemporaryDataFlowDebugSessionTests : IntegrationTest, IDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDataFlowDebugSessionTests" /> class.
        /// </summary>
        public TemporaryDataFlowDebugSessionTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _connection = TemporaryManagedIdentityConnection.Create(Configuration.GetServicePrincipal());
        }

        private DataFactoryConfig DataFactory => Configuration.GetDataFactory();

        [Fact]
        public async Task StartDebugSession_WithActiveSession_SucceedsByReusingSession()
        {
            // Arrange
            Guid activeSessionId, unknownSessionId = Guid.NewGuid();
            await using (var activeSession = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger, 
                options => options.ActiveSessionId = unknownSessionId))
            {
                activeSessionId = activeSession.SessionId;
                Assert.NotEqual(unknownSessionId, activeSessionId);

                // Act
                await using (var otherSession = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger, 
                    options => options.ActiveSessionId = activeSession.SessionId))
                {
                    // Assert
                    Assert.Equal(activeSession.SessionId, otherSession.SessionId);
                }

                await ShouldFindActiveSessionAsync(activeSession.SessionId);
            }

            await ShouldNotFindActiveSessionAsync(activeSessionId);
        }

        private async Task ShouldFindActiveSessionAsync(Guid sessionId)
        {
            bool isActive = await IsDebugSessionActiveAsync(sessionId);
            Assert.True(isActive, $"expected to have an active debug session in DataFactory '{DataFactory.Name}' for session ID: '{sessionId}', but got none");
        }

        private async Task ShouldNotFindActiveSessionAsync(Guid sessionId)
        {
            bool isActive = await IsDebugSessionActiveAsync(sessionId);
            Assert.False(isActive, $"expected to remove active debug session '{sessionId}' in DataFactory '{DataFactory.Name}', but it's still active");
        }

        private async Task<bool> IsDebugSessionActiveAsync(Guid sessionId)
        {
            var armClient = new ArmClient(new DefaultAzureCredential());
            DataFactoryResource resource = armClient.GetDataFactoryResource(DataFactory.ResourceId);

            var isActive = false;
            await foreach (DataFlowDebugSessionInfo session in resource.GetDataFlowDebugSessionsAsync())
            {
                if (session.SessionId == sessionId)
                {
                    isActive = true;
                }
            }

            return isActive;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
