using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    public class OnSetupBlobFileOptions
    {
        /// <summary>
        /// Gets the configured setup option on what to do with an existing Azure Blob file upon creation.
        /// </summary>
        /// <remarks>
        ///     [true] overrides the existing Azure Blob file when it already exists;
        ///     [false] uses the existing Azure Blob file's content instead.
        /// </remarks>
        internal bool OverrideBlob { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryBlobFile"/> to override an existing Azure Blob file when it already exists.
        /// </summary>
        public OnSetupBlobFileOptions OverrideExistingBlob()
        {
            OverrideBlob = true;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobFile"/> to use the existing Azure Blob file's content instead when it already exists.
        /// </summary>
        public OnSetupBlobFileOptions UseExistingBlob()
        {
            OverrideBlob = false;
            return this;
        }
    }

    /// <summary>
    /// Represents the available options when tearing down a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    internal enum OnTeardownBlob { DeleteIfCreated, DeleteIfExisted }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    public class OnTeardownBlobFileOptions
    {
        /// <summary>
        /// Gets the configured teardown option on what to do with the Azure Blob content upon disposal.
        /// </summary>
        internal OnTeardownBlob Content { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryBlobFile"/> to delete the Azure Blob file upon disposal if the test fixture created the file.
        /// </summary>
        public OnTeardownBlobFileOptions DeleteCreatedBlob()
        {
            Content = OnTeardownBlob.DeleteIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobFile"/> to delete the Azure Blob file upon disposal, even if it already existed - outside the fixture's scope.
        /// </summary>
        /// <returns></returns>
        public OnTeardownBlobFileOptions DeleteExistingBlob()
        {
            Content = OnTeardownBlob.DeleteIfExisted;
            return this;
        }
    }

    /// <summary>
    /// Represents the available options when uploading a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    public class TemporaryBlobFileOptions
    {
        /// <summary>
        /// Gets the additional options to manipulate the creation of the <see cref="TemporaryBlobFile"/>.
        /// </summary>
        public OnSetupBlobFileOptions OnSetup { get; } = new OnSetupBlobFileOptions().UseExistingBlob();

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryBlobFile"/>.
        /// </summary>
        public OnTeardownBlobFileOptions OnTeardown { get; } = new OnTeardownBlobFileOptions().DeleteCreatedBlob();
    }

    /// <summary>
    /// Represents a temporary Azure Blob file that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryBlobFile : IAsyncDisposable
    {
        private readonly bool _createdByUs;
        private readonly BinaryData _originalData;
        private readonly TemporaryBlobFileOptions _options;
        private readonly ILogger _logger;

        private TemporaryBlobFile(
            BlobClient blobClient,
            bool createdByUs,
            BinaryData originalData,
            TemporaryBlobFileOptions options,
            ILogger logger)
        {
            _createdByUs = createdByUs;
            _originalData = originalData;
            _options = options;
            _logger = logger ?? NullLogger.Instance;
            
            Client = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        }

        /// <summary>
        /// Gets the name of the Azure Blob file currently in storage.
        /// </summary>
        public string Name => Client.Name;

        /// <summary>
        /// Gets the name of the Azure Blob container where the Azure Blob file is currently stored.
        /// </summary>
        public string ContainerName => Client.BlobContainerName;

        /// <summary>
        /// Gets the client to interact with the temporary stored Azure Blob file currently in storage.
        /// </summary>
        public BlobClient Client { get; }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryBlobFile"/>.
        /// </summary>
        public OnTeardownBlobFileOptions OnTeardown => _options.OnTeardown;

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
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> or the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(Uri blobContainerUri, string blobName, BinaryData blobContent, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Requires a non-blank name for the Azure Blob file name for it to be uploaded to Azure Blob storage", nameof(blobName));
            }

            return await UploadIfNotExistsAsync(
                blobContainerUri ?? throw new ArgumentNullException(nameof(blobContainerUri)),
                blobName,
                blobContent ?? throw new ArgumentNullException(nameof(blobContent)),
                logger,
                configureOptions: null);
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
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> or the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(
            Uri blobContainerUri,
            string blobName,
            BinaryData blobContent,
            ILogger logger,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            if (blobContainerUri is null)
            {
                throw new ArgumentNullException(nameof(blobContainerUri));
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Requires a non-blank name for the Azure Blob file name for it to be uploaded to Azure Blob storage", nameof(blobName));
            }

            if (blobContent is null)
            {
                throw new ArgumentNullException(nameof(blobContent));
            }

            var containerClient = new BlobContainerClient(blobContainerUri, new DefaultAzureCredential());
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            return await UploadIfNotExistsAsync(blobClient, blobContent, logger, configureOptions);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobClient">The Azure Blob client to interact with Azure Blob storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(BlobClient blobClient, BinaryData blobContent, ILogger logger)
        {
            return await UploadIfNotExistsAsync(
                blobClient ?? throw new ArgumentNullException(nameof(blobClient)),
                blobContent ?? throw new ArgumentNullException(nameof(blobContent)),
                logger,
                configureOptions: null);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobClient">The Azure Blob client to interact with Azure Blob storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(
            BlobClient blobClient,
            BinaryData blobContent,
            ILogger logger,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            if (blobClient is null)
            {
                throw new ArgumentNullException(nameof(blobClient));
            }

            if (blobContent is null)
            {
                throw new ArgumentNullException(nameof(blobContent));
            }

            logger ??= NullLogger.Instance;
            var options = new TemporaryBlobFileOptions();
            configureOptions?.Invoke(options);

            (bool createdByUs, BinaryData originalData) = await EnsureBlobContentCreatedAsync(blobClient, blobContent, options, logger);

            return new TemporaryBlobFile(blobClient, createdByUs, originalData, options, logger);
        }

        private static async Task<(bool createdByUs, BinaryData originalData)> EnsureBlobContentCreatedAsync(
            BlobClient client, 
            BinaryData newContent,
            TemporaryBlobFileOptions options, 
            ILogger logger)
        {
            if (await client.ExistsAsync())
            {
                logger.LogDebug("Azure Blob '{BlobName}' already exists in container '{ContainerName}'", client.Name, client.BlobContainerName);
                BlobDownloadResult originalContent = await client.DownloadContentAsync();

                if (options.OnSetup.OverrideBlob)
                {
                    logger.LogDebug("Override existing Azure Blob '{BlobName}' in container '{ContainerName}'", client.Name, client.BlobContainerName);
                    await client.UploadAsync(newContent, overwrite: true);
                }

                return (createdByUs: false, originalContent.Content);
            }

            logger.LogTrace("Uploading Azure Blob '{BlobName}' to container '{ContainerName}'", client.Name, client.BlobContainerName);
            await client.UploadAsync(newContent);
            
            return (createdByUs: true, originalData: null);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (!_createdByUs && _originalData != null && _options.OnTeardown.Content != OnTeardownBlob.DeleteIfExisted)
            {
                _logger.LogDebug("Reverting Azure Blob '{BlobName}' original content in container '{ContainerName}'", Client.Name, Client.BlobContainerName);
                await Client.UploadAsync(_originalData, overwrite: true); 
            }

            if (_createdByUs || _options.OnTeardown.Content is OnTeardownBlob.DeleteIfExisted)
            {
                _logger.LogTrace("Deleting Azure Blob '{BlobName}' from container '{ContainerName}'", Client.Name, Client.BlobContainerName);
                await Client.DeleteIfExistsAsync();
            }
        }
    }
}
