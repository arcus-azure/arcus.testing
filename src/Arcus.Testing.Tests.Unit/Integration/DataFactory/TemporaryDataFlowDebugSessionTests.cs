using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture;
using Azure.ResourceManager.DataFactory;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public partial class TemporaryDataFlowDebugSessionTests
    {
        private readonly ILogger _logger;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDataFlowDebugSessionTests" /> class.
        /// </summary>
        public TemporaryDataFlowDebugSessionTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task StartDebugSession_WithDataFactoryResource_IsActiveDuringTestFixtureLifetime()
        {
            // Arrange
            var spyResource = new StubDataFactoryResource();

            // Act
            TemporaryDataFlowDebugSession session = await StartDebugSessionAsync(spyResource);

            // Assert
            Assert.True(spyResource.IsActive, "DataFlow debug session should be active after starting test fixture");
            Assert.Equal(spyResource.SessionId, session.SessionId);

            await session.DisposeAsync();
            Assert.False(spyResource.IsActive, "DataFlow debug session should be inactive after disposing test fixture");
        }

        private async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(DataFactoryResource resource, Action<TemporaryDataFlowDebugSessionOptions> configureOptions = null)
        {
            return configureOptions is null
                ? await TemporaryDataFlowDebugSession.StartDebugSessionAsync(resource, _logger)
                : await TemporaryDataFlowDebugSession.StartDebugSessionAsync(resource, _logger, configureOptions);
        }

        [Fact]
        public async Task StartDebugSession_WithoutDataFactoryResource_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => StartDebugSessionAsync(resource: null));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => StartDebugSessionAsync(resource: null, configureOptions: opt => { }));
        }

        [Fact]
        public void TimeToLive_WithNegativeValue_Fails()
        {
            // Arrange
            var options = new TemporaryDataFlowDebugSessionOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TimeToLiveInMinutes = Bogus.Random.Int(max: -1));
        }

        [Fact]
        public void ActiveSessionId_WithGuid_Succeeds()
        {
            // Arrange
            var options = new TemporaryDataFlowDebugSessionOptions();
            var value = Bogus.Random.Guid();

            // Act
            options.ActiveSessionId = value;

            // Assert
            Assert.Equal(value, options.ActiveSessionId);
        }

        [Fact]
        public void ActiveSessionId_WithEmptyGuid_Succeeds()
        {
            // Arrange
            var options = new TemporaryDataFlowDebugSessionOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.ActiveSessionId = Guid.Empty);
        }
    }
}
