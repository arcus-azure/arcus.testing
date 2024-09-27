using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryTableTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTableTests" /> class.
        /// </summary>
        public TemporaryTableTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempTable_OnNonExistingTable_SucceedsByExistingDuringLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = context.WhenTableUnavailable();
            
            // Act
            TemporaryTable temp = await CreateTempTableAsync(client);

            // Assert
            await context.ShouldStoreTableAsync(client);
            await temp.DisposeAsync();
            await context.ShouldNotStoreTableAsync(client);
        }

        [Fact]
        public async Task CreateTempTable_OnExistingTable_SucceedsByLeavingAfterLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();

            // Act
            TemporaryTable temp = await CreateTempTableAsync(client);

            // Assert
            await context.ShouldStoreTableAsync(client);
            await temp.DisposeAsync();
            await context.ShouldStoreTableAsync(client);
        }

        [Fact]
        public async Task CreateTempTable_OnNonExistingTableWhenTableIsDeletedOutsideFixture_SucceedsByIgnoringAlreadyDeletedTable()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = context.WhenTableUnavailable();
            TemporaryTable temp = await CreateTempTableAsync(client);
            await context.WhenTableDeletedAsync(client);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableAsync(client);
        }

        [Fact]
        public async Task CreateTempTable_OnExistingTableWithAddedEntity_SucceedsByRemovingEntityAfterLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TemporaryTable temp = await CreateTempTableAsync(client);

            TableEntity createdByUs = context.WhenTableEntityUnavailable();

            // Act
            await temp.AddEntityAsync(createdByUs);

            // Assert
            await context.ShouldStoreTableEntityAsync(client, createdByUs);
            await temp.DisposeAsync();
            await context.ShouldNotStoreTableEntityAsync(client, createdByUs);
        }

        private Task<TemporaryTable> CreateTempTableAsync(TableClient client)
        {
            return TemporaryTable.CreateIfNotExistsAsync(new Uri($"https://{client.Uri.Host}"), client.Name, Logger);
        }

        private async Task<TableStorageTestContext> GivenTableStorageAsync()
        {
            return await TableStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
