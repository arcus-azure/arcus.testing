using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryBlobContainer"/> is created.
    /// </summary>
    public enum UponCreation
    {
        /// <summary>
        /// (default) Do not perform any additional actions upon the creation of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        None = 0,

        /// <summary>
        /// Remove any existing blobs in the Azure Blob container upon the creation of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        Clean
    }

    /// <summary>
    /// Represents the available options when tracking data in the <see cref="TemporaryBlobContainer"/>.
    /// </summary>
    public enum DataTracking
    {
        /// <summary>
        /// (default) Marks that any deletion operation of the <see cref="TemporaryBlobContainer"/> should only happen
        /// upon data that the test fixture was responsible for creating, including the blobs and the container itself.
        /// </summary>
        Owned = 0,

        /// <summary>
        /// Marks that any deletion operation of the <see cref="TemporaryBlobContainer"/> should happen
        /// upon all data in the Azure Blob container, regardless whether the test fixture was responsible for creating the data - including the blobs and the container itself.
        /// </summary>
        Always = 1
    }

    /// <summary>
    /// Represents the available options when the <see cref="TemporaryBlobContainer"/> is deleted.
    /// </summary>
    public enum UponDeletion
    {
        /// <summary>
        /// (default) Remove the Azure Blob container upon the deletion of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        Remove = 2,

        /// <summary>
        /// Only clean the Azure Blob container upon the deletion of the <see cref="TemporaryBlobContainer"/>, not removing the container itself.
        /// </summary>
        Clean = 1
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryBlobContainer"/>.
    /// </summary>
    public class TemporaryBlobContainerOptions
    {
        /// <summary>
        ///   <param>Gets or sets the action to perform upon the creation of the <see cref="TemporaryBlobContainer"/></param>
        ///   <para>Default: <see cref="UponCreation.None"/>.</para>
        /// </summary>
        public UponCreation Creation { get; set; } = UponCreation.None;

        /// <summary>
        /// Gets or sets the behavior regards to tracking of data in the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        public DataTracking Tracking { get; set; } = DataTracking.Owned;

        /// <summary>
        /// Gets or sets the action to perform upon the deletion of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        public UponDeletion Deletion { get; set; } = UponDeletion.Remove;
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
            _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the name of the temporary Azure Blob container currently in storage.
        /// </summary>
        public string Name => _containerClient.Name;

        /// <summary>
        /// Gets the <see cref="BlobContainerClient"/> instance that represents the temporary Azure Blob container.
        /// </summary>
        public BlobContainerClient Client => _containerClient;

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob storage container if it doesn't exist yet.
        /// </summary>
        /// <param name="accountName">The name of the Azure Storage account to create the temporary Azure Blob container in.</param>
        /// <param name="containerName">The name of the Azure Blob container to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Blob container.</param>   
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="containerName"/> is blank.</exception>
        public static async Task<TemporaryBlobContainer> EnsureCreatedAsync(string accountName, string containerName, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Storage account name to create a temporary Azure Blob container test fixture," +
                    " used in container URI: 'https://{account_name}.blob.core.windows.net/{container_name}'", nameof(accountName));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Blob container name to create a temporary Azure Blob container test fixture," +
                    " used in container URI: 'https://{account_name}.blob.core.windows.net/{container_name}'", nameof(containerName));
            }

            var blobContainerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
            var containerClient = new BlobContainerClient(blobContainerUri, new DefaultAzureCredential());

            return await EnsureCreatedAsync(containerClient, logger ?? NullLogger.Instance);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob storage container if it doesn't exist yet.
        /// </summary>
        /// <param name="containerClient">The client to interact with the Azure Blob storage container.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the Azure Blob container.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<TemporaryBlobContainer> EnsureCreatedAsync(BlobContainerClient containerClient, ILogger logger)
        {
            if (containerClient is null)
            {
                throw new ArgumentNullException(nameof(containerClient));
            }

            logger ??= NullLogger.Instance;
           
            logger.LogDebug("Creating Azure Blob container '{ContainerName}'", containerClient.Name);
            await containerClient.CreateIfNotExistsAsync();

            return new TemporaryBlobContainer(containerClient, logger);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container with a random file name.
        /// </summary>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public async Task UploadBlobAsync(BinaryData blobContent)
        {
            await UploadBlobAsync(blobContent ?? throw new ArgumentNullException(nameof(blobContent)), configureOptions: null);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public async Task UploadBlobAsync(string blobName, BinaryData blobContent)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException($"Requires a non-blank blob name to upload a temporary blob in the temporary '{Name}' container", nameof(blobName));
            }

            await UploadBlobAsync(
                blobContent ?? throw new ArgumentNullException(nameof(blobContent)), 
                configureOptions: opt => opt.BlobName = blobName);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="configureOptions">The function to configure the additional options of how the blob should be uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public async Task UploadBlobAsync(BinaryData blobContent, Action<TemporaryBlobFileOptions> configureOptions)
        {
            if (blobContent is null)
            {
                throw new ArgumentNullException(nameof(blobContent));
            }

            _blobs.Add(await TemporaryBlobFile.UploadContentAsync(_containerClient, blobContent, _logger, configureOptions));
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