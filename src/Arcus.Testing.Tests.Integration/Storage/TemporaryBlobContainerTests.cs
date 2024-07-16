using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Storage.Blobs;
using Xunit;
using Xunit.Abstractions;

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
        public async Task CreateTempBlobContainer_WithoutExistingContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = context.WhenBlobContainerUnavailable();

            var container = await TemporaryBlobContainer.EnsureCreatedAsync(context.StorageAccount.Name, client.Name, Logger);
            await context.ShouldHaveCreatedBlobContainerAsync(client);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldHaveDeletedBlobContainerAsync(client);
        }

        [Fact]
        public async Task CreateTempBlobContainer_WithExistingContainer_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var context = await GivenBlobStorageAsync();

            BlobContainerClient client = await context.WhenBlobContainerAvailableAsync();

            var container = await TemporaryBlobContainer.EnsureCreatedAsync(context.StorageAccount.Name, client.Name, Logger);
            await context.ShouldHaveCreatedBlobContainerAsync(client);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldHaveCreatedBlobContainerAsync(client);
        }

        private async Task<BlobStorageTestContext> GivenBlobStorageAsync()
        {
            return await BlobStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
