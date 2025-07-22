using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Fixture;
using Azure.Core;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    [Collection(DataFactoryDebugSessionCollection.CollectionName)]
    public class TemporaryDataFlowDebugSessionTests : IntegrationTest
    {
        private readonly DataFactoryDebugSession _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDataFlowDebugSessionTests" /> class.
        /// </summary>
        public TemporaryDataFlowDebugSessionTests(DataFactoryDebugSession fixture, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _fixture = fixture;
        }

        private DataFactoryConfig DataFactory => Configuration.GetDataFactory();

        [Fact]
        public async Task StartDebugSession_WithActiveSession_SucceedsByReusingSession()
        {
            await using (var otherSession = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger,
                options => options.ActiveSessionId = _fixture.Value.SessionId))
            {
                // Assert
                Assert.Equal(_fixture.Value.SessionId, otherSession.SessionId);
            }

            await _fixture.ShouldFindActiveSessionAsync(_fixture.Value.SessionId);
        }
    }

    public class DataFactoryDataFlowDebugSessionDisposeTest : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFactoryDataFlowDebugSessionDisposeTest"/> class.
        /// </summary>
        public DataFactoryDataFlowDebugSessionDisposeTest(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task StartDebugSession_DisposeTwice_SucceedsByBeingRedundant()
        {
            // Arrange
            using var connection = TemporaryManagedIdentityConnection.Create(Configuration, Logger);

            DataFactoryConfig dataFactory = Configuration.GetDataFactory();
            ResourceIdentifier resourceId = dataFactory.ResourceId;
            var session = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(resourceId, Logger);
            Guid sessionId = session.SessionId;

            await session.DisposeAsync();

            // Act
            await session.DisposeAsync();

            // Assert
            bool isActive = await DataFactoryDebugSession.IsDebugSessionActiveAsync(resourceId, sessionId);
            Assert.False(isActive, $"expected to remove active debug session '{sessionId}' in DataFactory '{dataFactory.Name}', but it's still active");

            Assert.Throws<ObjectDisposedException>(() => session.SessionId);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.RunDataFlowAsync(Bogus.Lorem.Word(), Bogus.Lorem.Word()));
        }
    }
}
