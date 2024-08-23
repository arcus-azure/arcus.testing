using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Core.Integration.DataFactory;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    public class RunDataFlowTests : IntegrationTest, IDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunDataFlowTests" /> class.
        /// </summary>
        public RunDataFlowTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _connection = TemporaryManagedIdentityConnection.Create(ServicePrincipal);
        }

        public static IEnumerable<object[]> DataFlowJsonFormats => new[]
        {
            new object[] { JsonDocForm.SingleDoc, DataPreviewJson.GenerateJsonObject() },
            new object[] { JsonDocForm.ArrayOfDocs, DataPreviewJson.GenerateJsonArrayOfObjects(min: 2, max: 3) }
        };

        [Theory]
        [MemberData(nameof(DataFlowJsonFormats))]
        public async Task RunDataFlow_WithJsonFileOnSource_SucceedsByGettingJsonFileOnSink(JsonDocForm docForm, JsonNode expected)
        {
            // Arrange
            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithJsonSinkSourceAsync(JsonDocForm.ArrayOfDocs, Configuration, Logger);
            await dataFlow.UploadToSourceAsync(expected!.ToString());

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act
            DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            AssertJson.Equal(expected, result.GetDataAsJson());
        }

        [Fact]
        public async Task RunDataFlow_WithCsvFileOnSource_SucceedsByGettingCsvFileOnSink()
        {
            // Arrange
            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithCsvSinkSourceAsync(Configuration, Logger, ConfigureCsv);

            string expectedCsv = GenerateCsv();
            await dataFlow.UploadToSourceAsync(expectedCsv);

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act
            DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            CsvTable expected = AssertCsv.Load(expectedCsv, ConfigureCsv);
            AssertCsv.Equal(expected, result.GetDataAsCsv(ConfigureCsv));
        }

        private static string GenerateCsv()
        {
            var input = TestCsv.Generate(ConfigureCsv);
            return input.ToString();
        }

        private static void ConfigureCsv(AssertCsvOptions options)
        {
            options.Header = AssertCsvHeader.Present;
            options.Separator = ';';
            options.NewLine = Environment.NewLine;
        }

        private async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync()
        {
            DataFactoryConfig dataFactory = Configuration.GetDataFactory();
            return Bogus.Random.Bool()
                ? await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, Logger)
                : await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, Logger, opt => opt.TimeToLiveInMinutes = Bogus.Random.Int(10, 15));
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
