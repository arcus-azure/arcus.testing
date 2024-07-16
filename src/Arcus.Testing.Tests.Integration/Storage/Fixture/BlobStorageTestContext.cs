using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly ILogger _logger;

        private BlobStorageTestContext(TemporaryManagedIdentityConnection connection, BlobServiceClient serviceClient, ILogger logger)
        {
            _connection = connection;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        /// <summary>
        /// Gets the Azure Storage account that is used in the Azure Blob storage context.
        /// </summary>
        public StorageAccount StorageAccount => new(_serviceClient.AccountName);

        /// <summary>
        /// Creates a new <see cref="BlobStorageTestContext"/> that interacts with Azure Blob Storage.
        /// </summary>
        public static Task<BlobStorageTestContext> GivenAsync(TestConfig configuration, ILogger logger)
        {
            var connection = TemporaryManagedIdentityConnection.Create(configuration.GetServicePrincipal());
            var serviceClient = new BlobServiceClient(
                new Uri($"https://{configuration.GetStorageAccount().Name}.blob.core.windows.net"),
                new DefaultAzureCredential());

            return Task.FromResult(new BlobStorageTestContext(connection, serviceClient, logger));
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

        public BlobContainerClient WhenBlobContainerUnavailable()
        {
            var containerName = $"test{Guid.NewGuid():N}";

            BlobContainerClient containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _blobContainers.Add(containerClient);

            return containerClient;
        }

        public async Task ShouldHaveCreatedBlobContainerAsync(BlobContainerClient containerClient)
        {
            Assert.True(await containerClient.ExistsAsync(), "temporary blob container should be available when the test fixture is not disposed");
        }

        public async Task ShouldHaveDeletedBlobContainerAsync(BlobContainerClient containerClient)
        {
            Assert.False(await containerClient.ExistsAsync(), "temporary blob container should be unavailable when the test fixture is disposed");
        }

        /// <summary>
        /// Verifies that the blob file with the specified <paramref name="blobName"/> is stored in the <paramref name="containerClient"/>.
        /// </summary>
        public async Task ShouldStoreBlobFileAsync(BlobContainerClient containerClient, string blobName, BinaryData blobContent)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            Assert.True(await blobClient.ExistsAsync(), "temporary blob file should be available when the test fixture is not disposed");

            Response<BlobDownloadResult> getContent = await blobClient.DownloadContentAsync();
            Assert.Equal(blobContent.ToArray(), getContent.Value.Content.ToArray());
        }

        /// <summary>
        /// Verifies that the blob file with the specified <paramref name="blobName"/> is not stored in the <paramref name="containerClient"/>.
        /// </summary>
        public async Task ShouldNotStoreBlobFileAsync(BlobContainerClient containerClient, string blobName)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            Assert.False(await blobClient.ExistsAsync(), "temporary blob file should be unavailable when the test fixture is disposed");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            disposables.Add(_connection);

            foreach (BlobContainerClient container in _blobContainers)
            {
                disposables.Add(AsyncDisposable.Create(() => container.DeleteIfExistsAsync()));
            }
        }
    }
}