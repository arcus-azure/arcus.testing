using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure;
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
        public async Task UploadTempBlobFile_WithAvailableBlobContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BinaryData content = CreateBlobContent();

            TemporaryBlobFile file = await TemporaryBlobFile.UploadContentAsync(containerClient.Uri, content, Logger);
            await context.ShouldStoreBlobFileAsync(containerClient, file.FileName, content);

            // Act
            await file.DisposeAsync();

            // Assert
            await context.ShouldNotStoreBlobFileAsync(containerClient, file.FileName);
        }

        [Fact]
        public async Task UploadTempBlobFile_WithAlreadyUploadedBlobWithOverride_SucceedsByOverridingExistingBlob()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BinaryData content1 = CreateBlobContent();
            BinaryData content2 = CreateBlobContent();

            TemporaryBlobFile file1 = await TemporaryBlobFile.UploadContentAsync(containerClient, content1, Logger);
            TemporaryBlobFile file2 = await TemporaryBlobFile.UploadContentAsync(containerClient.Uri, content2, Logger, 
                options =>
                {
                    options.BlobName = file1.FileName;
                    options.OverrideExistingBlob = true;
                });

            // Assert
            await context.ShouldStoreBlobFileAsync(containerClient, file1.FileName, content2);
        }

        [Fact]
        public async Task UploadTempBlobFile_WithAlreadyUploadedBlobWithoutOverride_FailsWithConflict()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BinaryData content = CreateBlobContent();

            TemporaryBlobFile file1 = await TemporaryBlobFile.UploadContentAsync(containerClient.Uri, content, Logger);
            var exception = await Assert.ThrowsAnyAsync<RequestFailedException>(
                () => TemporaryBlobFile.UploadContentAsync(containerClient.Uri, content, Logger, options =>
                {
                    options.BlobName = file1.FileName;
                    options.OverrideExistingBlob = false;
                }));

            Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static BinaryData CreateBlobContent()
        {
            return BinaryData.FromBytes(Bogus.Random.Bytes(100));
        }

        private async Task<BlobStorageTestContext> GivenBlobStorageAsync()
        {
            return await BlobStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
