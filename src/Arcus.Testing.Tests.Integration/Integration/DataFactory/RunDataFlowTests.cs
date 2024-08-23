using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Core.Integration.DataFactory;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    public class RunDataFlowTests : IntegrationTest, IClassFixture<DataFactoryDebugSession>
    {
        private readonly DataFactoryDebugSession _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunDataFlowTests" /> class.
        /// </summary>
        public RunDataFlowTests(DataFactoryDebugSession session, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _session = session;
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
            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithJsonSinkSourceAsync(docForm, Configuration, Logger);
            await dataFlow.UploadToSourceAsync(expected!.ToString());

            // Act
            DataFlowRunResult result = await _session.Value.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

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

            // Act
            DataFlowRunResult result = await _session.Value.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

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
    }

    public class DataFactoryDebugSession : IAsyncLifetime
    {
        private readonly TestConfig _config;
        private TemporaryManagedIdentityConnection _connection;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFactoryDebugSession" /> class.
        /// </summary>
        public DataFactoryDebugSession()
        {
            _config = TestConfig.Create();
        }

        /// <summary>
        /// Gets the current value state of the debug session.
        /// </summary>
        public TemporaryDataFlowDebugSession Value { get; private set; }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _connection = TemporaryManagedIdentityConnection.Create(_config.GetServicePrincipal());

            DataFactoryConfig dataFactory = _config.GetDataFactory();
            Value = Bogus.Random.Bool()
                ? await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, NullLogger.Instance)
                : await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, NullLogger.Instance, opt => opt.TimeToLiveInMinutes = Bogus.Random.Int(10, 15));
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (Value != null)
            {
                await Value.DisposeAsync();
            }

            _connection?.Dispose();
        }
    }
}
