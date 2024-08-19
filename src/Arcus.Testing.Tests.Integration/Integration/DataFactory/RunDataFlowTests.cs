using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Xunit;
using Xunit.Abstractions;

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

        private DataFactoryConfig DataFactory => Configuration.GetDataFactory();

        [Fact]
        public async Task RunDataFlow_WithCsvFileOnSource_SucceedsByGettingCsvFileOnSink()
        {
            using var connection = TemporaryManagedIdentityConnection.Create(ServicePrincipal);
            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithCsvSinkSourceAsync(Configuration, Logger, ConfigureCsv);

            var input = TestCsv.Generate(ConfigureCsv);
            var expectedCsv = input.ToString();
            await dataFlow.UploadCsvToSourceAsync(expectedCsv);

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act
            DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            CsvTable expected = AssertCsv.Load(expectedCsv, ConfigureCsv);
            AssertCsv.Equal(expected, result.GetDataAsCsv(ConfigureCsv));
        }

        private static void ConfigureCsv(AssertCsvOptions options)
        {
            options.Header = AssertCsvHeader.Present;
            options.Separator = ';';
            options.NewLine = Environment.NewLine;
        }

        private async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync()
        {
            return Bogus.Random.Bool()
                ? await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger)
                : await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger, opt => opt.TimeToLiveInMinutes = Bogus.Random.Int(10, 15));
        }
    }
}
