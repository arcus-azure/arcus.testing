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

            TemporaryBlobFile file = await WhenBlobUploadedAsync(containerClient, blobContent: content, configureOptions: AnyOptions());
            await context.ShouldStoreBlobFileAsync(containerClient, file.Name, content);

            // Act
            await file.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, file.Name);
        }

        private static Action<TemporaryBlobFileOptions> AnyOptions()
        {
            if (Bogus.Random.Bool())
            {
                return null;
            }

            return options =>
            {
                if (Bogus.Random.Bool())
                {
                    options.OnSetup.OverrideExistingBlob();
                }
                else
                {
                    options.OnSetup.UseExistingBlob();
                }

                if (Bogus.Random.Bool())
                {
                    options.OnTeardown.DeleteCreatedBlob();
                }
                else
                {
                    options.OnTeardown.DeleteExistingBlob();
                }
            };
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

        [Fact]
        public async Task UploadTempBlobFile_WithAlreadyUploadedBlobWithoutOverride_SucceedsByUsingSameContent()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            
            BinaryData originalContent = context.CreateBlobContent();
            BlobClient existingBlob = await context.WhenBlobAvailableAsync(containerClient, blobContent: originalContent);
            BinaryData newContent = context.CreateBlobContent();

            // Act
            TemporaryBlobFile sut = await WhenBlobUploadedAsync(containerClient, existingBlob.Name, newContent, configureOptions: options =>
            {
                options.OnSetup.UseExistingBlob();
            });

            // Assert
            await context.ShouldStoreBlobFileAsync(containerClient, existingBlob.Name, originalContent);
            await sut.DisposeAsync();
            await context.ShouldStoreBlobFileAsync(containerClient, existingBlob.Name, originalContent);
        }

        [Fact]
        public async Task UploadTempBlobFileWithDeleteExisting_WithAlreadyUploadedBlob_SucceedsByRemovingUponDisposal()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BlobClient existingBlob = await context.WhenBlobAvailableAsync(containerClient);
            TemporaryBlobFile sut = await WhenBlobUploadedAsync(containerClient, existingBlob.Name, configureOptions: options =>
            {
                options.OnTeardown.DeleteExistingBlob();
            });

            // Act
            await sut.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, sut.Name);
        }

        [Fact]
        public async Task UploadTempBlobFileWithAllAvailableOutOfScopeOptions_OnExistingBlob_SucceedsByRemovingAll()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();

            BinaryData originalContent = context.CreateBlobContent();
            BlobClient existingBlob = await context.WhenBlobAvailableAsync(containerClient, blobContent: originalContent);

            BinaryData newContent = context.CreateBlobContent();
            TemporaryBlobFile sut = await WhenBlobUploadedAsync(containerClient, existingBlob.Name, newContent, configureOptions: options =>
            {
                options.OnSetup.UseExistingBlob();
                options.OnTeardown.DeleteExistingBlob();
            });
            await context.ShouldStoreBlobFileAsync(containerClient, sut.Name, originalContent);

            // Act
            await sut.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, existingBlob.Name);
        }

        private async Task<TemporaryBlobFile> WhenBlobUploadedAsync(
            BlobContainerClient client,
            string blobName = null,
            BinaryData blobContent = null,
            Action<TemporaryBlobFileOptions> configureOptions = null)
        {
            blobName ??= $"test-{Guid.NewGuid():N}";
            blobContent ??= BinaryData.FromBytes(Bogus.Random.Bytes(100));

            TemporaryBlobFile temp = configureOptions is null
                ? Bogus.Random.Bool() 
                    ? await TemporaryBlobFile.UploadIfNotExistsAsync(client.Uri, blobName, blobContent, Logger)
                    : await TemporaryBlobFile.UploadIfNotExistsAsync(client.GetBlobClient(blobName), blobContent, Logger)
                : Bogus.Random.Bool()
                    ? await TemporaryBlobFile.UploadIfNotExistsAsync(client.Uri, blobName, blobContent, Logger, configureOptions)
                    : await TemporaryBlobFile.UploadIfNotExistsAsync(client.GetBlobClient(blobName), blobContent, Logger, configureOptions);

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
