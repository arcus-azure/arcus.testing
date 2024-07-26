using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public class TemporaryDataFlowDebugSessionTests
    {
        private readonly ILogger _logger;

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
            var session = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(spyResource, _logger);

            // Assert
            Assert.True(spyResource.IsActive, "DataFlow debug session should be active after starting test fixture");
            await session.DisposeAsync();
            Assert.False(spyResource.IsActive, "DataFlow debug session should be inactive after disposing test fixture");
        }
    }

    public class StubDataFactoryResource : DataFactoryResource
    {
        public bool IsActive { get; private set; }

        public override DataFactoryData Data { get; } = new(AzureLocation.WestEurope);

        public override Task<ArmOperation<DataFactoryDataFlowCreateDebugSessionResult>> CreateDataFlowDebugSessionAsync(
            WaitUntil waitUntil,
            DataFactoryDataFlowDebugSessionContent content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = new Mock<DataFactoryDataFlowCreateDebugSessionResult>(
                "status",
                Guid.NewGuid(),
                new Dictionary<string, BinaryData>());

            var operation = new Mock<ArmOperation<DataFactoryDataFlowCreateDebugSessionResult>>();
            operation.Setup(r => r.Value)
                     .Returns(result.Object);

            IsActive = true;
            return Task.FromResult(operation.Object);
        }

        public override Task<Response> DeleteDataFlowDebugSessionAsync(
            DeleteDataFlowDebugSessionContent content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            IsActive = false;
            return Task.FromResult(Mock.Of<Response>());
        }
    }
}
