using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Moq;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture
{
    public class StubDataFactoryResource : DataFactoryResource
    {
        public bool IsActive { get; private set; }
        public Guid SessionId { get; } = Guid.NewGuid();
        public override ResourceIdentifier Id { get; } = ResourceIdentifier.Parse($"/subscriptions/{Guid.NewGuid()}/resourceGroups/{Guid.NewGuid()}/providers/Microsoft.DataFactory/factories/{Guid.NewGuid()}");
        public override DataFactoryData Data { get; } = new(AzureLocation.WestEurope);

        public override Task<ArmOperation<DataFactoryDataFlowCreateDebugSessionResult>> CreateDataFlowDebugSessionAsync(
            WaitUntil waitUntil,
            DataFactoryDataFlowDebugSessionContent content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = new Mock<DataFactoryDataFlowCreateDebugSessionResult>(
                "status",
                SessionId,
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