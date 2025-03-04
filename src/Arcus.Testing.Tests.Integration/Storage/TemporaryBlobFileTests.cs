using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Storage.Blobs;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.BlobStorage)]
    public class TemporaryBlobFileTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryBlobFileTests" /> class.
        /// </summary>
        public TemporaryBlobFileTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task UploadTempBlobFileCreatedByUs_WithAvailableBlobContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BinaryData content = context.CreateBlobContent();

            TemporaryBlobFile file = await WhenBlobUploadedAsync(containerClient, blobContent: content);
            await context.ShouldStoreBlobFileAsync(containerClient, file.Name, content);

            // Act
            await file.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, file.Name);
        }

        [Fact]
        public async Task UploadTempBlobFileDefault_WithAlreadyUploadedBlob_SucceedsByUsingExistingBlob()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BinaryData originalContent = context.CreateBlobContent();
            BinaryData newContent = context.CreateBlobContent();

            BlobClient existingBlob = await context.WhenBlobAvailableAsync(containerClient, blobContent: originalContent);
            TemporaryBlobFile sut = await WhenBlobUploadedAsync(containerClient, existingBlob.Name, newContent);

            // Assert
            await context.ShouldStoreBlobFileAsync(containerClient, existingBlob.Name, originalContent);
            await sut.DisposeAsync();
            await context.ShouldStoreBlobFileAsync(containerClient, existingBlob.Name, originalContent);
        }

        private async Task<TemporaryBlobFile> WhenBlobUploadedAsync(
            BlobContainerClient client,
            string blobName = null,
            BinaryData blobContent = null)
        {
            blobName ??= $"test-{Guid.NewGuid():N}";
            blobContent ??= BinaryData.FromBytes(Bogus.Random.Bytes(100));

            TemporaryBlobFile temp =
                Bogus.Random.Bool()
                    ? await TemporaryBlobFile.UploadIfNotExistsAsync(client.Uri, blobName, blobContent, Logger)
                    : await TemporaryBlobFile.UploadIfNotExistsAsync(client.GetBlobClient(blobName), blobContent, Logger);

            Assert.Equal(blobName, temp.Name);
            Assert.Equal(client.Name, temp.ContainerName);
            Assert.Equal(blobName, temp.Client.Name);
            Assert.Equal(client.Name, temp.Client.BlobContainerName);

            return temp;
        }

        private async Task<BlobStorageTestContext> GivenBlobStorageAsync()
        {
            return await BlobStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
