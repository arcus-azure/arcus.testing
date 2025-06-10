using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Core;
using Azure.Core.Expressions.DataFactory;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture
{
    /// <summary>
    /// Represents the available data types the DataFlow supports in Azure DataFactory.
    /// </summary>
    public enum DataFlowDataType { Csv, Json }

    /// <summary>
    /// Represents the available JSON settings in the source of the DataFlow.
    /// </summary>
    public enum JsonDocForm { SingleDoc, ArrayOfDocs }

    /// <summary>
    /// Represents a test fixture that temporary creates a DataFlow instance on an Azure DataFactory resource.
    /// </summary>
    public class TemporaryDataFactoryDataFlow : IAsyncDisposable
    {
        private readonly string _linkedServiceName;
        private readonly TestConfig _config;
        private readonly ArmClient _arm;
        private readonly DataFlowDataType _dataType;
        private readonly ILogger _logger;

        private TemporaryBlobContainer _sourceContainer;
        private DataFactoryLinkedServiceResource _linkedService;
        private DataFactoryDatasetResource _sourceDataset, _sinkDataset;
        private List<string> _flowletNames = new();
        private DataFactoryDataFlowResource _dataFlow;

        private TemporaryDataFactoryDataFlow(DataFlowDataType dataType, TestConfig config, ILogger logger)
        {
            _dataType = dataType;
            _linkedServiceName = RandomizeWith("storage");

            _arm = new ArmClient(new DefaultAzureCredential());
            _config = config;
            _logger = logger;

            var env = config.GetAzureEnvironment();
            SubscriptionId = env.SubscriptionId;
            ResourceGroupName = env.ResourceGroupName;

            Name = RandomizeWith("dataFlow");
            SourceName = RandomizeWith("sourceName");
            SinkName = RandomizeWith("sinkName");
            SinkDataSetName = RandomizeWith("sinkDataSet");
            SourceDataSetName = RandomizeWith("sourceDataSet");
        }

        private string SubscriptionId { get; }
        private string ResourceGroupName { get; }
        private DataFactoryConfig DataFactory => _config.GetDataFactory();
        private StorageAccount StorageAccount => _config.GetStorageAccount();

        /// <summary>
        /// Gets the unique name of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the unique name of the source of the temporary DataFlow in Azure DataFactory.
        /// This name is the name of the source "step" of the DataFlow, not to be mistaken with the source DataSet name.
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Gets the unique name of the sink of the temporary DataFlow in Azure DataFactory.
        /// This name is the name of the sink "step" of the DataFlow, not to be mistaken with the sink DataSet name.
        /// </summary>
        public string SinkName { get; }

        /// <summary>
        /// Gets the unique name of the source DataSet of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string SourceDataSetName { get; }

        /// <summary>
        /// Gets the unique name of the source DataSet of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string SinkDataSetName { get; }

        /// <summary>
        /// Creates a DataFlow with a CSV source and sink on an Azure DataFactory resource.
        /// </summary>
        public static async Task<TemporaryDataFactoryDataFlow> CreateWithCsvSinkSourceAsync(TestConfig config, ILogger logger, Action<AssertCsvOptions> configureOptions, Action<TempDataFlowOptions> tempDataFlowOptions = null)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            var dfOptions = new TempDataFlowOptions();
            tempDataFlowOptions?.Invoke(dfOptions);

            var temp = new TemporaryDataFactoryDataFlow(DataFlowDataType.Csv, config, logger);
            try
            {
                await temp.AddSourceBlobContainerAsync();
                await temp.AddLinkedServiceAsync();
                await temp.AddCsvSourceAsync(options, dfOptions);
                await temp.AddCsvSinkAsync(options, dfOptions);
                await temp.AddFlowletsAsync(dfOptions);
                await temp.AddDataFlowAsync(dataFlowOptions: dfOptions);
            }
            catch
            {
                await temp.DisposeAsync();
                throw;
            }

            return temp;
        }

        /// <summary>
        /// Creates a DataFlow with a JSON source and sink on an Azure DataFactory resource.
        /// </summary>
        public static async Task<TemporaryDataFactoryDataFlow> CreateWithJsonSinkSourceAsync(JsonDocForm docForm, TestConfig config, ILogger logger)
        {
            var temp = new TemporaryDataFactoryDataFlow(DataFlowDataType.Json, config, logger);
            try
            {
                await temp.AddSourceBlobContainerAsync();
                await temp.AddLinkedServiceAsync();
                await temp.AddJsonSourceAsync();
                await temp.AddJsonSinkAsync();
                await temp.AddDataFlowAsync(docForm);
            }
            catch
            {
                await temp.DisposeAsync();
                throw;
            }

            return temp;
        }

        private async Task AddSourceBlobContainerAsync()
        {
            _sourceContainer = await TemporaryBlobContainer.CreateIfNotExistsAsync(StorageAccount.Name, containerName: RandomizeWith("adf"), _logger);
        }

        private static string RandomizeWith(string label)
        {
            return label + Guid.NewGuid().ToString()[..5];
        }

        private async Task AddLinkedServiceAsync()
        {
            _logger.LogTrace("Adding Azure Blob storage linked service '{LinkedServiceName}' to Azure DataFactory '{DataFactoryName}'", _linkedServiceName, DataFactory.Name);

            ResourceIdentifier resourceId = DataFactoryLinkedServiceResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _linkedServiceName);
            _linkedService = _arm.GetDataFactoryLinkedServiceResource(resourceId);

            await _linkedService.UpdateAsync(WaitUntil.Completed, new DataFactoryLinkedServiceData(new AzureBlobStorageLinkedService
            {
                AuthenticationType = AzureStorageAuthenticationType.AccountKey,
                ConnectionString = StorageAccount.ConnectionString,
            }));
        }

        private async Task AddCsvSourceAsync(AssertCsvOptions options, TempDataFlowOptions dataFlowOptions)
        {
            _logger.LogTrace("Adding CSV source '{SourceName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SourceDataSetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sourceDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SourceDataSetName));

            var sourceProperties = new DelimitedTextDataset(blobStorageLinkedService)
            {
                ColumnDelimiter = options.Separator.ToString(),
                RowDelimiter = options.NewLine,
                QuoteChar = options.Quote.ToString(),
                FirstRowAsHeader = options.Header is AssertCsvHeader.Present,
                DataLocation = new AzureBlobStorageLocation()
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SourceDataSetName
                }
            };

            dataFlowOptions?.Source.ApplyOptions(sourceProperties, SourceDataSetName);

            await _sourceDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sourceProperties));
        }

        private async Task AddJsonSourceAsync()
        {
            _logger.LogTrace("Adding JSON source '{SourceName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SourceDataSetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sourceDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SourceDataSetName));

            var sourceProperties = new JsonDataset(blobStorageLinkedService)
            {
                DataLocation = new AzureBlobStorageLocation()
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SourceDataSetName
                }
            };
            await _sourceDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sourceProperties));
        }

        private async Task AddCsvSinkAsync(AssertCsvOptions options, TempDataFlowOptions dataFlowOptions)
        {
            _logger.LogTrace("Adding CSV sink '{SinkName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SinkDataSetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sinkDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SinkDataSetName));

            var sinkProperties = new DelimitedTextDataset(blobStorageLinkedService)
            {
                ColumnDelimiter = options.Separator.ToString(),
                RowDelimiter = options.NewLine,
                QuoteChar = options.Quote.ToString(),
                FirstRowAsHeader = options.Header is AssertCsvHeader.Present,
                DataLocation = new AzureBlobStorageLocation
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SinkDataSetName
                }
            };

            dataFlowOptions?.Sink.ApplyOptions(sinkProperties, SinkDataSetName);

            await _sinkDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sinkProperties));
        }

        private async Task AddJsonSinkAsync()
        {
            _logger.LogTrace("Adding JSON sink '{SinkName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", SinkDataSetName, Name, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sinkDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, SinkDataSetName));

            var sinkProperties = new JsonDataset(blobStorageLinkedService)
            {
                DataLocation = new AzureBlobStorageLocation
                {
                    Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                    FolderPath = SinkDataSetName
                }
            };
            await _sinkDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sinkProperties));
        }

        private async Task AddFlowletsAsync(TempDataFlowOptions dataFlowOptions)
        {
            if (dataFlowOptions?.FlowletNames.Count == 0)
            {
                return;
            }

            foreach (var flowletName in dataFlowOptions?.FlowletNames)
            {
                _logger.LogTrace("Adding Flowlet '{FlowletName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", flowletName, Name, DataFactory.Name);
                _flowletNames.Add(flowletName);

                var flowlet = GetFlowlet(SubscriptionId, ResourceGroupName, DataFactory.Name, _arm, flowletName);

                var properties = new DataFactoryFlowletProperties();
                // Have to clear both lists to make sure we have an instance of the list
                // ("sources": [], and "sinks": [], must be present in the typeProperties of the flowlet)
                properties.Sources.Clear();
                properties.Sinks.Clear();

                properties.Transformations.Add(new DataFlowTransformation("input1"));
                properties.Transformations.Add(new DataFlowTransformation("output1"));

                var lines = new List<string>
                {
                    "input(order: 0,",
                    "     allowSchemaDrift: true) ~> input1",
                    "input1 output() ~> output1"
                };
                foreach (var item in lines)
                {
                    properties.ScriptLines.Add(item);
                }

                await flowlet.UpdateAsync(WaitUntil.Completed, new DataFactoryDataFlowData(properties));
            }
        }

        private static DataFactoryDataFlowResource GetFlowlet(string subscriptionId, string resourceGroupName, string dataFactoryName, ArmClient _arm, string flowletName)
        {
            ResourceIdentifier flowletResourceId = DataFactoryDataFlowResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, dataFactoryName, flowletName);
            return _arm.GetDataFactoryDataFlowResource(flowletResourceId);
        }

        private async Task AddDataFlowAsync(JsonDocForm docForm = JsonDocForm.SingleDoc, TempDataFlowOptions dataFlowOptions = null)
        {
            _logger.LogTrace("Adding DataFlow '{DataFlowName}' to Azure DataFactory '{DataFactoryName}'", Name, DataFactory.Name);

            ResourceIdentifier dataFlowResourceId = DataFactoryDataFlowResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, Name);
            _dataFlow = _arm.GetDataFactoryDataFlowResource(dataFlowResourceId);

            var properties = new DataFactoryMappingDataFlowProperties
            {
                Sources =
                {
                    new DataFlowSource(SourceName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, SourceDataSetName)
                    }
                },
                Sinks =
                {
                    new DataFlowSink(SinkName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, SinkDataSetName)
                    }
                }
            };

            if (dataFlowOptions?.FlowletNames.Count > 0)
            {
                foreach (string flowletName in dataFlowOptions.FlowletNames)
                {
                    properties.Transformations.Add(
                        new DataFlowTransformation(flowletName)
                        {
                            Flowlet = new DataFlowReference(DataFlowReferenceType.DataFlowReference, flowletName)
                        }
                    );
                }
            }

            if (dataFlowOptions?.DataFlowParameters.Count > 0)
            {
                properties.Transformations.Add(new DataFlowTransformation("AnArbitraryDerivedColumnName"));
            }

            IEnumerable<string> scriptLines = _dataType switch
            {
                DataFlowDataType.Csv => DataFlowCsvScriptLines(SourceName, SinkName, dataFlowOptions?.FlowletNames, dataFlowOptions?.DataFlowParameters),
                DataFlowDataType.Json => DataFlowJsonScriptLines(SourceName, SinkName, docForm, dataFlowOptions?.FlowletNames, dataFlowOptions?.DataFlowParameters),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (string line in scriptLines)
            {
                properties.ScriptLines.Add(line);
            }

            await _dataFlow.UpdateAsync(WaitUntil.Completed, new DataFactoryDataFlowData(properties));
        }

        private static IEnumerable<string> DataFlowParametersScriptLines(IDictionary<string, string> dataFlowParameters)
        {
            IEnumerable<string> parameters = Enumerable.Empty<string>();
            if (dataFlowParameters?.Count > 0)
            {
                parameters = new[]
                {
                    "parameters{",
                    string.Join(",\n", dataFlowParameters.Select(p => $"     {p.Key} as {p.Value}")),
                    "}"
                };
            }

            return parameters;
        }

        private static IEnumerable<string> DataFlowCsvScriptLines(string sourceName, string sinkName, IList<string> flowletNames, IDictionary<string, string> dataFlowParameters)
        {
            IEnumerable<string> parameters = DataFlowParametersScriptLines(dataFlowParameters);

            return parameters.Concat(FormatDataFlowCsvScriptLines(sourceName, sinkName, flowletNames, dataFlowParameters));
        }

        private static string[] FormatDataFlowCsvScriptLines(string sourceName, string sinkName, IList<string> flowletNames, IDictionary<string, string> dataFlowParameters)
        {
            if (dataFlowParameters?.Count > 0)
            {
                return new[]
                {
                    "source(allowSchemaDrift: true,",
                    "       validateSchema: false,",
                    $"      ignoreNoFilesFound: false) ~> {sourceName}",
                    $"{sourceName} derive(",
                    string.Join(',', dataFlowParameters.Select(p => $"          {p.Key} = ${p.Key}")),
                    $"          ) ~> AnArbitraryDerivedColumnName",
                    $"AnArbitraryDerivedColumnName sink(allowSchemaDrift: true,",
                    "      validateSchema: false,",
                    "      skipDuplicateMapInputs: true,",
                    $"     skipDuplicateMapOutputs: true) ~> {sinkName}"
                };
            }

            if (flowletNames?.Count > 0)
            {
                return new[]
                {
                    "source(allowSchemaDrift: true,",
                    "       validateSchema: false,",
                    $"      ignoreNoFilesFound: false) ~> {sourceName}",
                    $"{sourceName} compose(composition: '{flowletNames.First()}') ~> {flowletNames.First()}@(output1)",
                    $"{flowletNames.First()}@output1 sink(allowSchemaDrift: true,",
                    "     validateSchema: false,",
                    "     skipDuplicateMapInputs: true,",
                    $"    skipDuplicateMapOutputs: true) ~> {sinkName}"
                };
            }

            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                $"     ignoreNoFilesFound: false) ~> {sourceName}",
                $"{sourceName} sink(allowSchemaDrift: true,",
                "      validateSchema: false,",
                "      skipDuplicateMapInputs: true,",
                $"     skipDuplicateMapOutputs: true) ~> {sinkName}"
            };
        }

        private static IEnumerable<string> DataFlowJsonScriptLines(string sourceName, string sinkName, JsonDocForm docForm, IList<string> flowletNames, IDictionary<string, string> dataFlowParameters)
        {
            IEnumerable<string> parameters = DataFlowParametersScriptLines(dataFlowParameters);

            return parameters.Concat(DataFlowJsonScriptLines(sourceName, sinkName, docForm, flowletNames, dataFlowParameters?.Count > 0));
        }

        private static string[] DataFlowJsonScriptLines(string sourceName, string sinkName, JsonDocForm docForm, IList<string> flowletNames, bool addDerivedColumWithDataFlowParams)
        {
            string documentForm = docForm switch
            {
                JsonDocForm.SingleDoc => "singleDocument",
                JsonDocForm.ArrayOfDocs => "arrayOfDocuments",
                _ => throw new ArgumentOutOfRangeException(nameof(docForm), docForm, null)
            };

            if (flowletNames?.Count > 0)
            {
                return new[]
                {
                    "source(allowSchemaDrift: true,",
                    "       validateSchema: false,",
                    $"      ignoreNoFilesFound: false) ~> {sourceName}",
                    $"{sourceName} compose(composition: '{flowletNames.First()}') ~> {flowletNames.First()}@(output1)",
                    $"{flowletNames.First()}@output1 sink(allowSchemaDrift: true,",
                    "     validateSchema: false,",
                    "     skipDuplicateMapInputs: true,",
                    $"    skipDuplicateMapOutputs: true) ~> {sinkName}"
                };
            }
            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     ignoreNoFilesFound: false,",
                $"    documentForm: '{documentForm}') ~> {sourceName}",
                addDerivedColumWithDataFlowParams ? $"{sourceName} derive(NewColumn = iif($BoolDataFlowParam, $StringDataFlowParam, toString($IntDataFlowParam))) ~> ADerivedColumn" : "",
                addDerivedColumWithDataFlowParams ? $"ADerivedColumn sink(allowSchemaDrift: true," : $"{sourceName} sink(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     skipDuplicateMapInputs: true,",
                $"    skipDuplicateMapOutputs: true) ~> {sinkName}"
            };
        }

        /// <summary>
        /// Uploads a file to the source of the temporary DataFlow.
        /// </summary>
        public async Task UploadToSourceAsync(string expected, params string[] subPath)
        {
            string fileExtension = _dataType switch
            {
                DataFlowDataType.Csv => ".csv",
                DataFlowDataType.Json => ".json",
                _ => throw new ArgumentOutOfRangeException()
            };

            string filePath = subPath.Aggregate(SourceDataSetName, Path.Combine);
            filePath = Path.Combine(filePath, RandomizeWith("input") + fileExtension);

            _logger.LogTrace("Upload {FileType} file to DataFlow source: {FileContents} with path: {filePath}", _dataType, expected, filePath);
            await _sourceContainer.UpsertBlobFileAsync(
                filePath,
                BinaryData.FromString(expected));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            if (_sourceContainer != null)
            {
                disposables.Add(_sourceContainer);
            }

            if (_dataFlow != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting DataFlow '{DataFlowName}' from Azure DataFactory '{DataFactoryName}'", Name, DataFactory.Name);
                    await _dataFlow.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sourceDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV source '{SourceName}' from Azure DataFactory '{DataFactoryName}'", SourceDataSetName, DataFactory.Name);
                    await _sourceDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sinkDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV sink '{SinkName}' from Azure DataFactory '{DataFactoryName}'", SinkDataSetName, DataFactory.Name);
                    await _sinkDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_flowletNames?.Count > 0)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    foreach (string flowletName in _flowletNames)
                    {
                        var flowlet = GetFlowlet(SubscriptionId, ResourceGroupName, DataFactory.Name, _arm, flowletName);
                        var flowletResource = await flowlet.GetAsync();
                        _logger.LogTrace("Deleting flowlet '{FlowletName}' from Azure DataFactory '{DataFactoryName}'", flowletResource.Value.Data.Name, DataFactory.Name);
                        await flowlet.DeleteAsync(WaitUntil.Completed);
                    }
                }));
            }

            if (_linkedService != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting Azure Blob storage linked service '{LinkedServiceName}' from Azure DataFactory '{DataFactoryName}'", _linkedServiceName, DataFactory.Name);
                    await _linkedService.DeleteAsync(WaitUntil.Completed);
                }));
            }
        }
    }

    public class TempDataFlowOptions
    {
        public TempDataFlowSourceOptions Source { get; } = new();
        public TempDataFlowSinkOptions Sink { get; } = new();
        /// <summary>
        /// Gets the unique key and values of the parameters of the temporary Data Flow in Azure DataFactory.
        /// </summary>
        public IDictionary<string, string> DataFlowParameters { get; } = new Dictionary<string, string>();
        public List<string> FlowletNames { get; } = new();
    }

    public class TempDataFlowSourceOptions
    {
        /// <summary>
        /// Gets the unique key and values of the parameters of the source DataSet of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public IDictionary<string, string> SourceDataSetParameterKeyValues { get; } = new Dictionary<string, string>();

        public TempDataFlowSourceOptions AddFolderPathParameters(IDictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                SourceDataSetParameterKeyValues.Add(parameter.Key, parameter.Value);
            }
            return this;
        }

        internal void ApplyOptions(DelimitedTextDataset dataSet, string sourceDataSetName)
        {
            string folderPathExpression = $"@concat('{sourceDataSetName}/', ";
            foreach (var sourceDataSetParametersKey in SourceDataSetParameterKeyValues.Keys)
            {
                folderPathExpression += $"dataset().{sourceDataSetParametersKey}, '/', ";
            }
            // Remove the last ", '/', " part only if we have parameters
            // Else just remove the last ", " part
            folderPathExpression = (SourceDataSetParameterKeyValues.Keys.Count > 0 ? folderPathExpression[..^7] : folderPathExpression[..^2]) + ")";

            if (SourceDataSetParameterKeyValues.Any())
            {
                dataSet.DataLocation.FolderPath = DataFactoryElement<string>.FromExpression(folderPathExpression);
            }

            foreach (var parameters in SourceDataSetParameterKeyValues)
            {
                dataSet.Parameters.Add(parameters.Key, new EntityParameterSpecification(EntityParameterType.String));
            }
        }
    }

    public class TempDataFlowSinkOptions
    {
        /// <summary>
        /// Gets the unique key and values of the parameters of the sink DataSet of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public IDictionary<string, string> SinkDataSetParameterKeyValues { get; } = new Dictionary<string, string>();

        public TempDataFlowSinkOptions AddFolderPathParameters(IDictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                SinkDataSetParameterKeyValues.Add(parameter.Key, parameter.Value);
            }
            return this;
        }

        internal void ApplyOptions(DelimitedTextDataset dataSet, string sinkDataSetName)
        {
            string folderPathExpression = $"@concat('{sinkDataSetName}/', ";
            foreach (var sinkDataSetParametersKey in SinkDataSetParameterKeyValues.Keys)
            {
                folderPathExpression += $"dataset().{sinkDataSetParametersKey}, '/', ";
            }
            // Remove the last ", '/', " part only if we have parameters
            // Else just remove the last ", " part
            folderPathExpression = (SinkDataSetParameterKeyValues.Keys.Count > 0 ? folderPathExpression[..^7] : folderPathExpression[..^2]) + ")";

            if (SinkDataSetParameterKeyValues.Any())
            {
                dataSet.DataLocation.FolderPath = DataFactoryElement<string>.FromExpression(folderPathExpression);
            }

            foreach (var parameters in SinkDataSetParameterKeyValues)
            {
                dataSet.Parameters.Add(parameters.Key, new EntityParameterSpecification(EntityParameterType.String));
            }
        }
    }
}
