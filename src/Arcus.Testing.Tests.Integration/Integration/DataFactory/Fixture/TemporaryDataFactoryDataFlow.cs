using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory.Models;
using Azure.ResourceManager.DataFactory;
using Microsoft.Extensions.Logging;
using Azure.Core.Expressions.DataFactory;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory.Fixture
{
    public class TemporaryDataFactoryDataFlow : IAsyncDisposable
    {
        private readonly string _dataFlowName, _sinkDatasetName, _sourceDatasetName, _linkedServiceName;
        private readonly TestConfig _config;
        private readonly ArmClient _arm;
        private TemporaryBlobContainer _sourceContainer;
        private readonly ILogger _logger;

        private DataFactoryLinkedServiceResource _linkedService;
        private DataFactoryDatasetResource _sourceDataset, _sinkDataset;
        private DataFactoryDataFlowResource _dataFlow;

        private TemporaryDataFactoryDataFlow(TestConfig config, ILogger logger)
        {
            _dataFlowName = RandomizeWith("dataFlow");
            _sinkDatasetName = RandomizeWith("sink");
            _sourceDatasetName = RandomizeWith("source");
            _linkedServiceName = RandomizeWith("storage");

            _arm = new ArmClient(new DefaultAzureCredential());
            _config = config;
            _logger = logger;
        }

        private string SubscriptionId => _config["Arcus:SubscriptionId"];

        private string ResourceGroupName => _config["Arcus:ResourceGroup:Name"];

        private DataFactoryConfig DataFactory => _config.GetDataFactory();
        private StorageAccount StorageAccount => _config.GetStorageAccount();

        public string Name => _dataFlowName;

        public string SinkName => "dataflowsink";

        public string SourceName => _sourceDatasetName;

        public static async Task<TemporaryDataFactoryDataFlow> CreateWithCsvSinkSourceAsync(TestConfig config, ILogger logger, Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            var temp = new TemporaryDataFactoryDataFlow(config, logger);
            try
            {
                await temp.AddSourceBlobContainerAsync();
                await temp.AddLinkedServiceAsync();
                await temp.AddCsvSourceAsync(options);
                await temp.AddCsvSinkAsync(options);
                await temp.AddDataFlowAsync();
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

        private async Task AddCsvSourceAsync(AssertCsvOptions options)
        {
            _logger.LogTrace("Adding CSV source '{SourceName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", _sourceDatasetName, _dataFlowName, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sourceDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _sourceDatasetName));
            var sourceProperties = new DelimitedTextDataset(blobStorageLinkedService);
            sourceProperties.ColumnDelimiter = ";";
            sourceProperties.RowDelimiter = options.NewLine;
            sourceProperties.QuoteChar = "\"";
            sourceProperties.FirstRowAsHeader = options.Header is AssertCsvHeader.Present;
            sourceProperties.DataLocation = new AzureBlobStorageLocation()
            {
                Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                FolderPath = _sourceDatasetName
            };
            await _sourceDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sourceProperties));
        }

        private async Task AddCsvSinkAsync(AssertCsvOptions options)
        {
            _logger.LogTrace("Adding CSV sink '{SinkName}' to DataFlow '{DataFlowName}' within Azure DataFactory '{DataFactoryName}'", _sinkDatasetName, _dataFlowName, DataFactory.Name);

            var blobStorageLinkedService = new DataFactoryLinkedServiceReference(DataFactoryLinkedServiceReferenceKind.LinkedServiceReference, _linkedServiceName);

            _sinkDataset = _arm.GetDataFactoryDatasetResource(DataFactoryDatasetResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _sinkDatasetName));
            var sinkProperties = new DelimitedTextDataset(blobStorageLinkedService);
            sinkProperties.ColumnDelimiter = ";";
            sinkProperties.RowDelimiter = options.NewLine;
            sinkProperties.QuoteChar = "\"";
            sinkProperties.FirstRowAsHeader = options.Header is AssertCsvHeader.Present;

            sinkProperties.DataLocation = new AzureBlobStorageLocation
            {
                Container = _sourceContainer?.Name ?? throw new InvalidOperationException("Azure blob storage container should be available at this point"),
                FolderPath = _sinkDatasetName
            };
            await _sinkDataset.UpdateAsync(WaitUntil.Completed, new DataFactoryDatasetData(sinkProperties));
        }

        private async Task AddDataFlowAsync()
        {
            _logger.LogTrace("Adding DataFlow '{DataFlowName}' to Azure DataFactory '{DataFactoryName}'", _dataFlowName, DataFactory.Name);

            ResourceIdentifier dataFlowResourceId = DataFactoryDataFlowResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, DataFactory.Name, _dataFlowName);
            _dataFlow = _arm.GetDataFactoryDataFlowResource(dataFlowResourceId);

            var sourceName = "dataflowsource";
            await _dataFlow.UpdateAsync(WaitUntil.Completed, new DataFactoryDataFlowData(new DataFactoryMappingDataFlowProperties
            {
                Sources =
                {
                    new DataFlowSource(sourceName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, _sourceDatasetName)
                    }
                },
                Sinks =
                {
                    new DataFlowSink(SinkName)
                    {
                        Dataset = new DatasetReference(DatasetReferenceType.DatasetReference, _sinkDatasetName)
                    }
                },
                ScriptLines =
                {
                    "source(allowSchemaDrift: true,",
                    "     validateSchema: false,",
                    $"     ignoreNoFilesFound: false) ~> {sourceName}",
                    $"{sourceName} sink(allowSchemaDrift: true,",
                    "     validateSchema: false,",
                    "     skipDuplicateMapInputs: true,",
                    $"     skipDuplicateMapOutputs: true) ~> {SinkName}"
                }
            }));
        }

        public async Task UploadCsvToSourceAsync(string expected)
        {
            await _sourceContainer.UploadBlobAsync(Path.Combine(SourceName, RandomizeWith("input") + ".csv"), BinaryData.FromString(expected));
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
                    _logger.LogTrace("Deleting DataFlow '{DataFlowName}' from Azure DataFactory '{DataFactoryName}'", _dataFlowName, DataFactory.Name);
                    await _dataFlow.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sourceDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV source '{SourceName}' from Azure DataFactory '{DataFactoryName}'", _sourceDatasetName, DataFactory.Name);
                    await _sourceDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_sinkDataset != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting CSV sink '{SinkName}' from Azure DataFactory '{DataFactoryName}'", _sinkDatasetName, DataFactory.Name);
                    await _sinkDataset.DeleteAsync(WaitUntil.Completed);
                }));
            }

            if (_linkedService != null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting Azure Blob storage linked service '{LinkedServiceName}' from Azure DataFactory '{DataFactoryName}'", _linkedService, DataFactory.Name);
                    await _linkedService.DeleteAsync(WaitUntil.Completed);
                }));
            }
        }
    }
}
