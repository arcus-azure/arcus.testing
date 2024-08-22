using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture;
using Bogus;
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

        [Fact]
        public async Task RunDataFlow_WithJsonFileOnSource_SucceedsByGettingJsonFileOnSink()
        {
            // Arrange
//            var expected = JsonNode.Parse(
//@"[{
//    ""productId"": 123,
//    ""description"": ""this is a description"",
//    ""tags"": [ ""tag1"", ""tag2"" ],
//    ""related"": [ { ""productId"": 456, ""tags"": [ ""tag3"", ""tag4"" ] } ],
//    ""category"": { ""name"": ""these products"" }
//},
//{
//    ""productId"": 123,
//    ""description"": ""this is a description"",
//    ""tags"": [ ""tag1"", ""tag2"" ],
//    ""related"": [ { ""productId"": 456, ""tags"": [ ""tag3"", ""tag4"" ] } ],
//    ""category"": { ""name"": ""these products"" }
//}]");

            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithJsonSinkSourceAsync(JsonDocForm.ArrayOfDocs, Configuration, Logger);

            JsonNode expected = GenerateJson();

            await dataFlow.UploadToSourceAsync(expected!.ToString());

            await using TemporaryDataFlowDebugSession session = await StartDebugSessionAsync();

            // Act
            DataFlowRunResult result = await session.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            AssertJson.Equal(expected, result.GetDataAsJson());
        }

        private static JsonNode GenerateJson()
        {
            return Bogus.Random.Bool()
                ? JsonNode.Parse(TestJson.GenerateObject().ToString())
                : JsonSerializer.SerializeToNode(Bogus.Make(Bogus.Random.Int(2, 5), TestJson.GenerateObject));
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
