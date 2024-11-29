using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Core.Integration.DataFactory;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture;
using Azure.Identity;
using Azure.ResourceManager.DataFactory.Models;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    [Collection(DataFactoryDebugSessionCollection.CollectionName)]
    public class RunDataFlowTests : IntegrationTest
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
            await dataFlow.UploadToSourceAsync(expected!.ToString(), null);

            // Act
            DataFlowRunResult result = await _session.Value.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            AssertJson.Equal(expected, result.GetDataAsJson());
        }

        [Fact]
        public async Task RunDataFlow_WithCsvFileOnSource_SucceedsByGettingCsvFileOnSink()
        {
            // Arrange
            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithCsvSinkSourceAsync(Configuration, Logger, ConfigureCsv, null);

            string expectedCsv = GenerateCsv();
            await dataFlow.UploadToSourceAsync(expectedCsv, null);

            // Act
            DataFlowRunResult result = await _session.Value.RunDataFlowAsync(dataFlow.Name, dataFlow.SinkName);

            // Assert
            CsvTable expected = AssertCsv.Load(expectedCsv, ConfigureCsv);
            AssertCsv.Equal(expected, result.GetDataAsCsv(ConfigureCsv));
        }

        [Fact]
        public async Task RunDataFlow_WithCsvFileOnSourceAndDataSetParameter_SucceedsByGettingCsvFileOnSinkWithSubPathParameter()
        {
            // Arrange
            IDictionary<string, string> sourceDataSetParameterKeyValues = new Dictionary<string, string>
            {
                { RandomizeWith("sourceDataSetParameterKey"), RandomizeWith("SourceDataSetParameterValue") },
                { RandomizeWith("sourceDataSetParameterKey"), RandomizeWith("SourceDataSetParameterValue") }
            };
            IDictionary<string, string> sinkDataSetParameterKeyValues = new Dictionary<string, string>
            {
                { RandomizeWith("sinkDataSetParameterKey"), RandomizeWith("sinkDataSetParameterValue") }
            };

            await using var dataFlow = await TemporaryDataFactoryDataFlow.CreateWithCsvSinkSourceAsync(Configuration, Logger, ConfigureCsv, dataFlowOptions =>
            {
                dataFlowOptions.Source.AddFolderPathParameters(sourceDataSetParameterKeyValues);
                dataFlowOptions.Sink.AddFolderPathParameters(sinkDataSetParameterKeyValues);
            });

            string expectedCsv = GenerateCsv();
            await dataFlow.UploadToSourceAsync(expectedCsv, dataFlow.SourceDataSetParameterKeyValues.Select(d => d.Value).ToArray());

            // Act
            DataFlowRunResult result = await _session.Value.RunDataFlowAsync(
                dataFlow.Name,
                dataFlow.SinkName,
                options =>
                {
                    foreach (var sourceDataSetParameter in sourceDataSetParameterKeyValues)
                    {
                        options.AddDataSetParameter(dataFlow.SourceName, sourceDataSetParameter.Key, sourceDataSetParameter.Value);
                    }
                    foreach (var sinkDataSetParameter in sinkDataSetParameterKeyValues)
                    {
                        options.AddDataSetParameter(dataFlow.SinkName, sinkDataSetParameter.Key, sinkDataSetParameter.Value);
                    }
                }
            );

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
        private static string RandomizeWith(string label)
        {
            return label + Guid.NewGuid().ToString()[..5];
        }
    }

    [CollectionDefinition(CollectionName)]
    public class DataFactoryDebugSessionCollection : ICollectionFixture<DataFactoryDebugSession>
    {
        public const string CollectionName = "Active DataFactory debug session";

        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
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

        private DataFactoryConfig DataFactory => _config.GetDataFactory();
        private Guid SessionId { get; set; }

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
            Guid unknownSessionId = Guid.NewGuid();

            Value = Bogus.Random.Bool()
                ? await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, NullLogger.Instance)
                : await TemporaryDataFlowDebugSession.StartDebugSessionAsync(dataFactory.ResourceId, NullLogger.Instance, opt =>
                {
                    opt.TimeToLiveInMinutes = Bogus.Random.Int(10, 15);
                    opt.ActiveSessionId = unknownSessionId;
                });

            Assert.NotEqual(unknownSessionId, Value.SessionId);
            SessionId = Value.SessionId;
        }

        public async Task ShouldFindActiveSessionAsync(Guid sessionId)
        {
            bool isActive = await IsDebugSessionActiveAsync(sessionId);
            Assert.True(isActive, $"expected to have an active debug session in DataFactory '{DataFactory.Name}' for session ID: '{sessionId}', but got none");
        }

        public async Task ShouldNotFindActiveSessionAsync(Guid sessionId)
        {
            bool isActive = await IsDebugSessionActiveAsync(sessionId);
            Assert.False(isActive, $"expected to remove active debug session '{sessionId}' in DataFactory '{DataFactory.Name}', but it's still active");
        }

        private async Task<bool> IsDebugSessionActiveAsync(Guid sessionId)
        {
            var armClient = new ArmClient(new DefaultAzureCredential());
            DataFactoryResource resource = armClient.GetDataFactoryResource(DataFactory.ResourceId);

            var isActive = false;
            await foreach (DataFlowDebugSessionInfo session in resource.GetDataFlowDebugSessionsAsync())
            {
                if (session.SessionId == sessionId)
                {
                    isActive = true;
                }
            }

            return isActive;
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            await using var disposables = new DisposableCollection(NullLogger.Instance);

            if (Value != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await Value.DisposeAsync();
                    await ShouldNotFindActiveSessionAsync(SessionId);
                }));
            }

            disposables.Add(_connection);
        }
    }
}
