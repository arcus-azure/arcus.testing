using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    /// <summary>
    /// Represents a test context instance that provides meaningful interaction points with Azure Blob storage.
    /// </summary>
    public class BlobStorageTestContext : IAsyncDisposable
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly Collection<BlobContainerClient> _blobContainers = new();
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private BlobStorageTestContext(BlobServiceClient serviceClient, StorageAccount storageAccount, ILogger logger)
        {
            _serviceClient = serviceClient;
            _logger = logger;

            StorageAccount = storageAccount;
        }

        /// <summary>
        /// Gets the Azure Storage account that is used in the Azure Blob storage context.
        /// </summary>
        public StorageAccount StorageAccount { get; }

        /// <summary>
        /// Creates a new <see cref="BlobStorageTestContext"/> that interacts with Azure Blob Storage.
        /// </summary>
        public static Task<BlobStorageTestContext> GivenAsync(TestConfig configuration, ILogger logger)
        {
            StorageAccount storageAccount = configuration.GetStorageAccount();
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{storageAccount.Name}.blob.core.windows.net"),
                new DefaultAzureCredential());

            return Task.FromResult(new BlobStorageTestContext(serviceClient, storageAccount, logger));
        }

        /// <summary>
        /// Provides a new Azure Blob container that is available for the duration of the test.
        /// </summary>
        public async Task<BlobContainerClient> WhenBlobContainerAvailableAsync()
        {
            BlobContainerClient containerClient = WhenBlobContainerUnavailable();

            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        /// <summary>
        /// Provides a new Azure Blob container that is not yet created.
        /// </summary>
        public BlobContainerClient WhenBlobContainerUnavailable()
        {
            var containerName = $"test{Guid.NewGuid():N}";

            BlobContainerClient containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _blobContainers.Add(containerClient);

            return containerClient;
        }

        /// <summary>
        /// Provides a new Azure Blob file on the specified <paramref name="containerClient"/>.
        /// </summary>
        public async Task<BlobClient> WhenBlobAvailableAsync(BlobContainerClient containerClient, string blobName = null, BinaryData blobContent = null)
        {
            blobName ??= $"blob{Guid.NewGuid():N}";
            blobContent ??= CreateBlobContent();

            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(blobContent);

            return blobClient;
        }

        /// <summary>
        /// Creates a new blob content with random bytes.
        /// </summary>
        public BinaryData CreateBlobContent()
        {
            return BinaryData.FromBytes(Bogus.Random.Bytes(100));
        }

        /// <summary>
        /// Verifies that the blob container with the specified <paramref name="containerClient"/> is still available.
        /// </summary>
        public async Task ShouldStoreBlobContainerAsync(BlobContainerClient containerClient)
        {
            Assert.True(await containerClient.ExistsAsync(), $"temporary blob container '{containerClient.Name}' should remain available");
        }

        /// <summary>
        /// Verifies that the blob container with the specified <paramref name="containerClient"/> is deleted.
        /// </summary>
        public async Task ShouldDeleteBlobContainerAsync(BlobContainerClient containerClient)
        {
            Assert.False(await containerClient.ExistsAsync(), $"temporary blob container '{containerClient.Name}' should be deleted");
        }

        /// <summary>
        /// Verifies that the blob file with the specified <paramref name="blobName"/> is stored in the <paramref name="containerClient"/>.
        /// </summary>
        public async Task ShouldStoreBlobFileAsync(BlobContainerClient containerClient, string blobName, BinaryData blobContent)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            Assert.True(await blobClient.ExistsAsync(), $"temporary blob file '{blobName}' should be available in container {containerClient.Name}");

            Response<BlobDownloadResult> getContent = await blobClient.DownloadContentAsync();
            Assert.Equal(blobContent.ToArray(), getContent.Value.Content.ToArray());
        }

        /// <summary>
        /// Verifies that the blob file with the specified <paramref name="blobName"/> is stored in the <paramref name="containerClient"/>.
        /// </summary>
        public async Task ShouldStoreBlobFileAsync(BlobContainerClient containerClient, string blobName)
        {
            Assert.True(await containerClient.GetBlobClient(blobName).ExistsAsync(), $"temporary blob file '{blobName}' should be available in container '{containerClient.Name}'");
        }

        /// <summary>
        /// Verifies that the blob file with the specified <paramref name="blobName"/> is not stored in the <paramref name="containerClient"/>.
        /// </summary>
        public async Task ShouldDeleteBlobFileAsync(BlobContainerClient containerClient, string blobName)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            Assert.False(await blobClient.ExistsAsync(), $"temporary blob file '{blobName}' should be unavailable in container '{containerClient.Name}'");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            foreach (BlobContainerClient container in _blobContainers)
            {
                disposables.Add(AsyncDisposable.Create(() => container.DeleteIfExistsAsync()));
            }
        }
    }
}
