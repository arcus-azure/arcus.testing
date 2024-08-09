using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public partial class TemporaryDataFlowDebugSessionTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task RunDataFlow_WithoutDataFlowName_Fails(string dataFlowName)
        {
            // Arrange
            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => session.RunDataFlowAsync(dataFlowName, "<sink-name>"));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => session.RunDataFlowAsync(dataFlowName, "<sink-name>", opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task RunDataFlow_WithoutTargetSinkName_Fails(string targetSinkName)
        {
            // Arrange
            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => session.RunDataFlowAsync("<data-flow-name>", targetSinkName));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => session.RunDataFlowAsync("<data-flow-name>", targetSinkName, opt => { }));
        }

        private async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync()
        {
            return await StartDebugSessionAsync(new StubDataFactoryResource());
        }

        [Fact]
        public void MaxRows_WithNegativeValue_Fails()
        {
            // Arrange
            var options = new RunDataFlowOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MaxRows = Bogus.Random.Int(max: -1));
        }
    }
}
