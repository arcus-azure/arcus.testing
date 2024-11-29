using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private DataFactoryDataFlowResource _dataFlow;

        private TemporaryDataFactoryDataFlow(DataFlowDataType dataType, TestConfig config, ILogger logger)
        {
            _dataType = dataType;
            _linkedServiceName = RandomizeWith("storage");

            _arm = new ArmClient(new DefaultAzureCredential());
            _config = config;
            _logger = logger;

            Name = RandomizeWith("dataFlow");
            SourceName = RandomizeWith("sourceName");
            SinkName = RandomizeWith("sinkName");
            SinkDataSetName = RandomizeWith("sinkDataSet");
            SourceDataSetName = RandomizeWith("sourceDataSet");
            SinkDataSetParameterValue = RandomizeWith("sinkDataSetParameterValue");
        }

        private string SubscriptionId => _config["Arcus:SubscriptionId"];
        private string ResourceGroupName => _config["Arcus:ResourceGroup:Name"];
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
        /// Gets the unique name of the parameter value of the sink DataSet of the temporary DataFlow in Azure DataFactory.
        /// </summary>
        public string SinkDataSetParameterValue { get; }

        /// <summary>
        /// Creates a DataFlow with a CSV source and sink on an Azure DataFactory resource.
        /// </summary>
        public static async Task<TemporaryDataFactoryDataFlow> CreateWithCsvSinkSourceAsync(TestConfig config, ILogger logger, Action<AssertCsvOptions> configureOptions, Action<TempDataFlowOptions> tempDataFlowOptions)
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
                await temp.AddDataFlowAsync();
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

            dataFlowOptions?.Source.ApplyOptions(ref sourceProperties, SourceDataSetName);
            dataFlowOptions?.Source.AddDataSetParameters(ref sourceProperties, dataFlowOptions?.Source.SourceDataSetParameterKeyValues);

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

            dataFlowOptions?.Sink.ApplyOptions(ref sinkProperties, SinkDataSetName);
            dataFlowOptions?.Sink.AddDataSetParameters(ref sinkProperties, dataFlowOptions?.Sink.SinkDataSetParameterKeyValues);

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

        private async Task AddDataFlowAsync(JsonDocForm docForm = JsonDocForm.SingleDoc)
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

            IEnumerable<string> scriptLines = _dataType switch
            {
                DataFlowDataType.Csv => DataFlowCsvScriptLines(SourceName, SinkName),
                DataFlowDataType.Json => DataFlowJsonScriptLines(SourceName, SinkName, docForm),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (string line in scriptLines)
            {
                properties.ScriptLines.Add(line);
            }

            await _dataFlow.UpdateAsync(WaitUntil.Completed, new DataFactoryDataFlowData(properties));
        }

        private static IEnumerable<string> DataFlowCsvScriptLines(string sourceName, string sinkName)
        {
            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                $"     ignoreNoFilesFound: false) ~> {sourceName}",
                $"{sourceName} sink(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     skipDuplicateMapInputs: true,",
                $"     skipDuplicateMapOutputs: true) ~> {sinkName}"
            };
        }

        private static IEnumerable<string> DataFlowJsonScriptLines(string sourceName, string sinkName, JsonDocForm docForm)
        {
            string documentForm = docForm switch
            {
                JsonDocForm.SingleDoc => "singleDocument",
                JsonDocForm.ArrayOfDocs => "arrayOfDocuments",
                _ => throw new ArgumentOutOfRangeException(nameof(docForm), docForm, null)
            };

            return new[]
            {
                "source(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     ignoreNoFilesFound: false,",
                $"    documentForm: '{documentForm}') ~> {sourceName}",
                $"{sourceName} sink(allowSchemaDrift: true,",
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
            await _sourceContainer.UploadBlobAsync(
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

        internal void ApplyOptions(ref DelimitedTextDataset dataSet, string sourceDataSetName)
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
        }

        internal void AddDataSetParameters(ref DelimitedTextDataset dataSet, IDictionary<string, string> sourceDataSetParameterKeyValues)
        {
            foreach (var parameters in sourceDataSetParameterKeyValues)
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

        internal void ApplyOptions(ref DelimitedTextDataset dataSet, string sinkDataSetName)
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
        }

        internal void AddDataSetParameters(ref DelimitedTextDataset dataSet, IDictionary<string, string> sinkDataSetParameterKeyValues)
        {
            foreach (var parameters in sinkDataSetParameterKeyValues)
            {
                dataSet.Parameters.Add(parameters.Key, new EntityParameterSpecification(EntityParameterType.String));
            }
        }
    }
}
