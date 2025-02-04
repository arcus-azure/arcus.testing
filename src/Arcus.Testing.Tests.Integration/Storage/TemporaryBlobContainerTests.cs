using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Storage.Blobs;
using Xunit;
using Xunit.Abstractions;
#pragma warning disable CS0618 // Ignore obsolete warnings that we added ourselves, should be removed upon releasing v2.0.
using static Arcus.Testing.BlobNameFilter;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.BlobStorage)]
    public class TemporaryBlobContainerTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryBlobContainerTests" /> class.
        /// </summary>
        public TemporaryBlobContainerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempBlobContainer_OnNonExistingContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = context.WhenBlobContainerUnavailable();
            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, client);
            await context.ShouldStoreBlobContainerAsync(client);

            string blobCreatedByUs = await UploadBlobAsync(context, container);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(client, blobCreatedByUs);
            await context.ShouldDeleteBlobContainerAsync(client);
        }

        [Fact]
        public async Task CreateTempBlobContainer_OnExistingContainer_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = await context.WhenBlobContainerAvailableAsync();
            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, client);

            await context.ShouldStoreBlobContainerAsync(client);
            string blobCreatedByUs = await UploadBlobAsync(context, container);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldStoreBlobContainerAsync(client);
            await context.ShouldDeleteBlobFileAsync(client, blobCreatedByUs);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithCleanIfExistedUponCreation_OnExistingContainer_SucceedsByRemovingBlobsInContainer()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();
            BlobClient blobOutsideOurScope = await context.WhenBlobAvailableAsync(containerClient);

            // Act
            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, containerClient, options =>
            {
                options.OnSetup.CleanAllBlobs();
            });

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, blobOutsideOurScope.Name);

            string blobCreatedByUs = await UploadBlobAsync(context, container);
            await container.DisposeAsync();
            await context.ShouldDeleteBlobFileAsync(containerClient, blobCreatedByUs);

            await context.ShouldStoreBlobContainerAsync(containerClient);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithCleanMatchingBlobsUponCreation_OnExistingContainer_SucceedsByRemovingOnlyMatchingBlobs()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();

            BlobClient matchingBlob = await context.WhenBlobAvailableAsync(containerClient);
            BlobClient notMatchingBlob = await context.WhenBlobAvailableAsync(containerClient);

            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, containerClient, options =>
            {
                if (Bogus.Random.Bool())
                {
                    options.OnSetup.CleanMatchingBlobs(DeprecatedMatching(matchingBlob.Name));
                }
                else
                {
                    options.OnSetup.CleanMatchingBlobs(blob => blob.Name == matchingBlob.Name);
                }
            });

            await context.ShouldDeleteBlobFileAsync(containerClient, matchingBlob.Name);
            await context.ShouldStoreBlobFileAsync(containerClient, notMatchingBlob.Name);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldStoreBlobFileAsync(containerClient, notMatchingBlob.Name);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithCleanMatchingUponDeletion_OnExistingContainer_SucceedsByRemovingOnlyMatchingBlobs()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();

            BlobClient matchingBlob = await context.WhenBlobAvailableAsync(containerClient);
            BlobClient notMatchingBlob = await context.WhenBlobAvailableAsync(containerClient);

            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, containerClient, options =>
            {
                if (Bogus.Random.Bool())
                {
                    options.OnTeardown.CleanMatchingBlobs(DeprecatedMatching(matchingBlob.Name));
                }
                else
                {
                    options.OnTeardown.CleanMatchingBlobs(blob => blob.Name == matchingBlob.Name);
                }
            });
            string blobCreatedByUs = await UploadBlobAsync(context, container);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, blobCreatedByUs);
            await context.ShouldDeleteBlobFileAsync(containerClient, matchingBlob.Name);
            await context.ShouldStoreBlobFileAsync(containerClient, notMatchingBlob.Name);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithCleanMatchingUponCreationAndDeletion_OnExistingContainer_SucceedsByRemovingOnlyMatchingBlobs()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();

            BlobClient matchingCreationBlob = await context.WhenBlobAvailableAsync(containerClient);
            string matchingDeletionBlobName = $"blob-{Guid.NewGuid()}";

            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, containerClient, options =>
            {
                if (Bogus.Random.Bool())
                {
                    options.OnSetup.CleanMatchingBlobs(DeprecatedMatching(matchingCreationBlob.Name));
                }
                else
                {
                    options.OnSetup.CleanMatchingBlobs(blob => blob.Name == matchingCreationBlob.Name);
                }
            });
            container.OnTeardown.CleanMatchingBlobs(DeprecatedMatching(matchingDeletionBlobName));

            await context.ShouldDeleteBlobFileAsync(containerClient, matchingCreationBlob.Name);
            BlobClient matchingDeletionBlob = await context.WhenBlobAvailableAsync(containerClient, matchingDeletionBlobName);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, matchingDeletionBlob.Name);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithAllAvailableOutOfScopeOptions_OnExistingContainer_SucceedsByRemovingAll()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient containerClient = await context.WhenBlobContainerAvailableAsync();

            BlobClient blobCreationOutsideScope = await context.WhenBlobAvailableAsync(containerClient);
            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, containerClient, options =>
            {
                options.OnSetup.CleanAllBlobs();
                options.OnTeardown.CleanAllBlobs().DeleteExistingContainer();
            });
            BlobClient blobDeletionOutsideScope = await context.WhenBlobAvailableAsync(containerClient);

            await context.ShouldDeleteBlobFileAsync(containerClient, blobCreationOutsideScope.Name);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(containerClient, blobDeletionOutsideScope.Name);
            await context.ShouldDeleteBlobContainerAsync(containerClient);
        }

        private static BlobNameFilter DeprecatedMatching(string name)
        {
            return Bogus.Random.Int(1, 4) switch
            {
                1 => Bogus.Random.Bool() ? NameEqual(name) : NameEqual(RandomCase(name), StringComparison.OrdinalIgnoreCase),
                2 => Bogus.Random.Bool() ? NameStartsWith(name[..10]) : NameStartsWith(RandomCase(name[..10]), StringComparison.OrdinalIgnoreCase),
                3 => Bogus.Random.Bool() ? NameEndsWith(name[^10..]) : NameEndsWith(RandomCase(name[^10..]), StringComparison.OrdinalIgnoreCase),
                4 => Bogus.Random.Bool() ? NameContains(name[1..10]) : NameContains(RandomCase(name[1..10]), StringComparison.OrdinalIgnoreCase),
                _ => throw new InvalidOperationException($"Unsupported '{nameof(BlobNameFilter)}' method, please provide one in the switch expression")
            };
        }

        private static string RandomCase(string value)
        {
            return new string(value.Select(c => Bogus.Random.Bool() ? char.ToUpper(c) : char.ToLower(c)).ToArray());
        }

        [Fact]
        public async Task CreateTempBlobContainerWithCleanAllBlobs_OnExistingContainerWithBlob_SucceedsByCleaningContainerUponDisposal()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = await context.WhenBlobContainerAvailableAsync();
            BlobClient blobOutsideOurScope = await context.WhenBlobAvailableAsync(client);

            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, client, options =>
            {
                options.OnTeardown.CleanAllBlobs();
            });
            string blobCreatedByUs = await UploadBlobAsync(context, container);


            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobFileAsync(client, blobOutsideOurScope.Name);
            await context.ShouldDeleteBlobFileAsync(client, blobCreatedByUs);
            await context.ShouldStoreBlobContainerAsync(client);
        }

        [Fact]
        public async Task CreateTempBlobContainerWithDeleteExistingContainer_OnExistingContainer_SucceedsByRemovingContainerUponDisposal()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = await context.WhenBlobContainerAvailableAsync();
            TemporaryBlobContainer container = await WhenTempContainerCreatedAsync(context, client, options =>
            {
                options.OnTeardown.DeleteExistingContainer();
            });

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldDeleteBlobContainerAsync(client);
        }

        private static async Task<string> UploadBlobAsync(BlobStorageTestContext context, TemporaryBlobContainer container)
        {
            string blobName = $"test-{Guid.NewGuid()}";
            BlobClient client = await container.UploadBlobAsync(blobName, context.CreateBlobContent());
            return client.Name;
        }

        private async Task<TemporaryBlobContainer> WhenTempContainerCreatedAsync(
            BlobStorageTestContext context,
            BlobContainerClient client,
            Action<TemporaryBlobContainerOptions> configureOptions = null)
        {
#pragma warning disable S3358 // Sonar suggests extracting nested condition, but that will create the container twice + does not help with  readability.

            TemporaryBlobContainer temp = configureOptions is null
                ? Bogus.Random.Bool()
                    ? await TemporaryBlobContainer.CreateIfNotExistsAsync(context.StorageAccount.Name, client.Name, Logger)
                    : await TemporaryBlobContainer.CreateIfNotExistsAsync(client, Logger)
                : Bogus.Random.Bool()
                    ? await TemporaryBlobContainer.CreateIfNotExistsAsync(context.StorageAccount.Name, client.Name, Logger, configureOptions)
                    : await TemporaryBlobContainer.CreateIfNotExistsAsync(client, Logger, configureOptions);

#pragma warning restore

            Assert.Equal(client.Name, temp.Name);
            Assert.Equal(client.Name, temp.Client.Name);
            Assert.Equal(client.AccountName, temp.Client.AccountName);

            return temp;
        }

        private async Task<BlobStorageTestContext> GivenBlobStorageAsync()
        {
            return await BlobStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
