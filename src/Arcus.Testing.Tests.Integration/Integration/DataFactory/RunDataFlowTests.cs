using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static Arcus.Testing.TemporaryDataFlowDebugSession;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    public class RunDataFlowTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunDataFlowTests" /> class.
        /// </summary>
        public RunDataFlowTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private StorageAccount StorageAccount => Configuration.GetStorageAccount();
        private DataFactoryConfig DataFactory => Configuration.GetDataFactory();

        [Fact]
        public async Task RunDataFlowCsv_OnDataFlowDebugSession_SucceedsWithResult()
        {
            // Arrange
            using var connection = TemporaryManagedIdentityConnection.Create(ServicePrincipal);
            
            DataFlowConfig dataFlow = DataFactory.DataFlowCsv;
            await using var source = await TemporaryBlobContainer.CreateIfNotExistsAsync(StorageAccount.Name, dataFlow.Source.ContainerName, Logger);

            var expectedCsv = TestCsv.Generate().ToString();
            await source.UploadBlobAsync(dataFlow.Source.FileName, BinaryData.FromString(expectedCsv));

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync(DataFactory.ResourceId, Logger);

            // Act
           DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            AssertCsv.Equal(AssertCsv.Load(expectedCsv), result.GetDataAsCsv());
        }

        [Fact]
        public async Task RunDataFlowJson_OnDataFlowDebugSession_SucceedsWithResult()
        {
            // Arrange
            using var connection = TemporaryManagedIdentityConnection.Create(ServicePrincipal);

            DataFlowConfig dataFlow = DataFactory.DataFlowJson;
            await using var source = await TemporaryBlobContainer.CreateIfNotExistsAsync(StorageAccount.Name, dataFlow.Source.ContainerName, Logger);

            var expectedJson = TestJson.Generate().ToString();
            await source.UploadBlobAsync(dataFlow.Source.FileName, BinaryData.FromString(expectedJson));

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync(DataFactory.ResourceId, Logger);

            // Act
            DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            AssertJson.Equal(expectedJson, result.Data.ToString());
        }
    }
}
