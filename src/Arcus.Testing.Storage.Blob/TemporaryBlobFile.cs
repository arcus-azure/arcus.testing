using System;
using System.Threading.Tasks;
using Azure.Identity;
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
        private string _blobName = $"test-{Guid.NewGuid()}";

        /// <summary>
        /// Gets or sets the name of the Azure Blob file to upload.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string BlobName
        {
            get => _blobName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank Azure Blob name to upload a blob to the storage", nameof(value));
                }

                _blobName = value;
            }
        }

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
            _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the name of the Azure Blob file currently in storage.
        /// </summary>
        public string FileName => _blobClient.Name;

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob storage.
        /// </remarks>
        /// <param name="blobContainerUri">
        ///     A <see cref="BlobContainerClient.Uri" /> referencing the blob container that includes the name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadContentAsync(Uri blobContainerUri, BinaryData blobContent, ILogger logger)
        {
            if (blobContainerUri is null)
            {
                throw new ArgumentNullException(nameof(blobContainerUri));
            }

            return await UploadContentAsync(blobContainerUri, blobContent, logger, configureOptions: null);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob storage.
        /// </remarks>
        /// <param name="blobContainerUri">
        ///     A <see cref="BlobContainerClient.Uri" /> referencing the blob container that includes the name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadContentAsync(Uri blobContainerUri, BinaryData blobContent, ILogger logger, Action<TemporaryBlobFileOptions> configureOptions)
        {
            if (blobContainerUri is null)
            {
                throw new ArgumentNullException(nameof(blobContainerUri));
            }

            return await UploadContentAsync(new BlobContainerClient(blobContainerUri, new DefaultAzureCredential()), blobContent, logger, configureOptions);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="containerClient">The Azure Blob container client to interact with Azure Blob storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="containerClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadContentAsync(BlobContainerClient containerClient, BinaryData blobContent, ILogger logger)
        {
            return await UploadContentAsync(containerClient, blobContent, logger, configureOptions: null);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="containerClient">The Azure Blob container client to interact with Azure Blob storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="containerClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadContentAsync(BlobContainerClient containerClient, BinaryData blobContent, ILogger logger, Action<TemporaryBlobFileOptions> configureOptions)
        {
            if (containerClient is null)
            {
                throw new ArgumentNullException(nameof(containerClient));
            }

            if (blobContent is null)
            {
                throw new ArgumentNullException(nameof(blobContent));
            }

            logger ??= NullLogger.Instance;
            var options = new TemporaryBlobFileOptions();
            configureOptions?.Invoke(options);

            BlobClient blobClient = containerClient.GetBlobClient(options.BlobName);

            logger.LogDebug("Uploading Azure Blob '{BlobName}' to container '{ContainerName}'", options.BlobName, blobClient.BlobContainerName);
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
}
