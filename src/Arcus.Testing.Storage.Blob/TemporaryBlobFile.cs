using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CS0618, S1133 // Ignore obsolete warnings that we added ourselves, should be removed upon releasing v2.0.

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryBlobFile"/>.
    /// </summary>
    [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
              "whether or not existing/non-existing files should be cleaned during setup/teardown, options will be removed in v2.0")]
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
    [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
              "whether or not existing/non-existing files should be cleaned during setup/teardown, options will be removed in v2.0")]
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
    [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
              "whether or not existing/non-existing files should be cleaned during setup/teardown, options will be removed in v2.0")]
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
            ArgumentNullException.ThrowIfNull(blobClient);
            ArgumentNullException.ThrowIfNull(options);

            _createdByUs = createdByUs;
            _originalData = originalData;
            _options = options;
            _logger = logger ?? NullLogger.Instance;

            Client = blobClient;
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
        [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
                  "whether or not existing/non-existing files should be cleaned during setup/teardown")]
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
                blobContainerUri,
                blobName,
                blobContent,
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
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
                  "whether or not existing/non-existing files should be cleaned during setup/teardown, this overload with options will be removed in v2.0")]
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(
            Uri blobContainerUri,
            string blobName,
            BinaryData blobContent,
            ILogger logger,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(blobContainerUri);
            ArgumentNullException.ThrowIfNull(blobContent);

            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Requires a non-blank name for the Azure Blob file name for it to be uploaded to Azure Blob storage", nameof(blobName));
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
            return await UploadIfNotExistsAsync(blobClient, blobContent, logger, configureOptions: null);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobClient">The Azure Blob client to interact with Azure Blob storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        [Obsolete("Use the '" + nameof(TemporaryBlobContainerOptions) + "' instead on Azure Blob storage container-level to control " +
                  "whether or not existing/non-existing files should be cleaned during setup/teardown, this overload with options will be removed in v2.0")]
        public static async Task<TemporaryBlobFile> UploadIfNotExistsAsync(
            BlobClient blobClient,
            BinaryData blobContent,
            ILogger logger,
            Action<TemporaryBlobFileOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(blobClient);
            ArgumentNullException.ThrowIfNull(blobContent);
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
                BlobDownloadResult originalContent = await client.DownloadContentAsync();

                if (options.OnSetup.OverrideBlob)
                {
                    logger.LogDebug("[Test:Setup] Replace already existing Azure Blob file '{BlobName}' in container '{AccountName}/{ContainerName}'", client.Name, client.AccountName, client.BlobContainerName);
                    await client.UploadAsync(newContent, overwrite: true);
                }

                return (createdByUs: false, originalContent.Content);
            }

            logger.LogDebug("[Test:Setup] Upload Azure Blob file '{BlobName}' to container '{AccountName}/{ContainerName}'", client.Name, client.AccountName, client.BlobContainerName);
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
                _logger.LogDebug("[Test:Teardown] Revert replaced Azure Blob file '{BlobName}' to original content in container '{AccountName}/{ContainerName}'", Client.Name, Client.AccountName, Client.BlobContainerName);
                await Client.UploadAsync(_originalData, overwrite: true);
            }

            if (_createdByUs || _options.OnTeardown.Content is OnTeardownBlob.DeleteIfExisted)
            {
                _logger.LogDebug("[Test:Teardown] Delete Azure Blob file '{BlobName}' from container '{AccountName}/{ContainerName}'", Client.Name, Client.AccountName, Client.BlobContainerName);
                await Client.DeleteIfExistsAsync();
            }

            GC.SuppressFinalize(this);
        }
    }
}