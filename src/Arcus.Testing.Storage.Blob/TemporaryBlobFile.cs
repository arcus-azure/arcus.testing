using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when uploading a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    public class TemporaryBlobFileOptions
    {
        /// <summary>
        /// Gets or sets the flag indicating whether or not to override an existing blob with the same name (default: <c>true</c>).
        /// </summary>
        public bool OverrideExistingBlob { get; set; } = true;
    }

    /// <summary>
    /// Represents a temporary Azure Blob file that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryBlobFile : IAsyncDisposable
    {
        private readonly BlobClient _blobClient;
        private readonly ILogger _logger;

        private TemporaryBlobFile(BlobClient blobClient, ILogger logger)
        {
            _blobClient = blobClient;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerClient"></param>
        /// <param name="blobName"></param>
        /// <param name="blobContent"></param>
        /// <param name="logger"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static async Task<TemporaryBlobFile> UploadAsync(
            BlobContainerClient containerClient,
            string blobName,
            BinaryData blobContent, 
            ILogger logger,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            logger ??= NullLogger.Instance;

            var options = new TemporaryBlobFileOptions();
            configureOptions?.Invoke(options);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            logger.LogDebug("Uploading Azure Blob '{BlobName}' to container '{ContainerName}'", blobName, blobClient.BlobContainerName);
            await blobClient.UploadAsync(blobContent, options.OverrideExistingBlob);

            return new TemporaryBlobFile(blobClient, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogDebug("Deleting Azure Blob '{BlobName}' from container '{ContainerName}'", _blobClient.Name, _blobClient.BlobContainerName);
            await _blobClient.DeleteIfExistsAsync();
        }
    }

    /// <summary>
    /// Represents a temporary Azure Blob container that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryBlobContainer : IAsyncDisposable
    {
        private readonly BlobContainerClient _containerClient;
        private readonly Collection<TemporaryBlobFile> _blobs = new Collection<TemporaryBlobFile>();
        private readonly ILogger _logger;

        private TemporaryBlobContainer(BlobContainerClient containerClient, ILogger logger)
        {
            _containerClient = containerClient;
            _logger = logger ?? NullLogger.Instance;
        }

        public static async Task<TemporaryBlobContainer> CreateAsync(
            BlobServiceClient serviceClient,
            string containerName,
            ILogger logger)
        {
            logger ??= NullLogger.Instance;

            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);

            logger.LogDebug("Creating Azure Blob container '{ContainerName}'", containerName);
            await containerClient.CreateIfNotExistsAsync();

            return new TemporaryBlobContainer(containerClient, logger);
        }

        public async Task UploadBlobAsync(
            string blobName,
            BinaryData blobContent,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            _blobs.Add(await TemporaryBlobFile.UploadAsync(_containerClient, blobName, blobContent, _logger, configureOptions));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.AddRange(_blobs);
            disposables.Add(AsyncDisposable.Create(async () =>
            {
                _logger.LogDebug("Deleting Azure Blob container '{ContainerName}'", _containerClient.Name);
                await _containerClient.DeleteIfExistsAsync();
            }));
        }
    }
}
