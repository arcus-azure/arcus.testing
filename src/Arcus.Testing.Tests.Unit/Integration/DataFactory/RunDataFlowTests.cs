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

        [Fact]
        public void AddDataFlowParameter_WithExistingName_SucceedsByOverriding()
        {
            // Arrange
            var options = new RunDataFlowOptions();
            string name = Bogus.Lorem.Word();
            options.AddDataFlowParameter(name, Bogus.Random.Guid());

            // Act
            options.AddDataFlowParameter(name, Bogus.Random.Guid());

            // Assert
            Assert.NotNull(options);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddDataFlowParameter_WithoutName_Fails(string name)
        {
            // Arrange
            var options = new RunDataFlowOptions();
            string value = Bogus.Lorem.Word();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddDataFlowParameter(name, value));
        }

        [Fact]
        public void AddDataFlowParameter_WithoutUnsupportedJson_Fails()
        {
            // Arrange
            var options = new RunDataFlowOptions();
            string name = Bogus.Lorem.Word();
            Type invalidValue = GetType();

            // Act / Assert
            Assert.ThrowsAny<NotSupportedException>(() => options.AddDataFlowParameter(name, invalidValue));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddDataSetParameter_WithoutDataSetName_Fails(string dataSetName)
        {
            // Arrange
            var options = new RunDataFlowOptions();
            string name = Bogus.Lorem.Word();
            string value = Bogus.Lorem.Word();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddDataSetParameter(dataSetName, name, value));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddDataSetParameter_WithoutParameterName_Fails(string name)
        {
            // Arrange
            var options = new RunDataFlowOptions();
            string dataSetName = Bogus.Lorem.Word();
            string value = Bogus.Lorem.Word();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddDataSetParameter(dataSetName, name, value));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddLinkedService_WithoutServiceName_Fails(string serviceName)
        {
            // Arrange
            var options = new RunDataFlowOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddLinkedService(serviceName));
        }
    }
}
