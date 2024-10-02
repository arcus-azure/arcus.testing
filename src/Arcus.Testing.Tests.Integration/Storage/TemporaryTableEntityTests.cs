using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryTableEntityTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTableEntityTests" /> class.
        /// </summary>
        public TemporaryTableEntityTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempTableEntity_OnNonExistingEntity_SucceedsByExistingDuringLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity entity = context.WhenTableEntityUnavailable();

            // Act
            TemporaryTableEntity temp = await CreateTempTableEntityAsync(client, entity);

            // Assert
            await context.ShouldStoreTableEntityAsync(client, entity);
            await temp.DisposeAsync();
            await context.ShouldNotStoreTableEntityAsync(client, entity);
        }

        [Fact]
        public async Task CreateTempTableEntity_OnExistingEntity_SucceedsByLeavingAfterLifetime()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity original = await context.WhenTableEntityAvailableAsync(client);

            TableEntity current = context.WhenTableEntityUnavailable();
            current.RowKey = original.RowKey;
            current.PartitionKey = original.PartitionKey;

            // Act
            TemporaryTableEntity temp = await CreateTempTableEntityAsync(client, current);

            // Assert
            await context.ShouldStoreTableEntityAsync(client, current);
            await temp.DisposeAsync();
            await context.ShouldStoreTableEntityAsync(client, original);
        }

        [Fact]
        public async Task CreateTempTableEntity_OnNonExistingTableEntityWhenEntityIsDeletedOutsideFixture_SucceedsByIgnoringAlreadyDeletedEntity()
        {
            // Arrange
            await using TableStorageTestContext context = await GivenTableStorageAsync();

            TableClient client = await context.WhenTableAvailableAsync();
            TableEntity entity = context.WhenTableEntityUnavailable();
            TemporaryTableEntity temp = await CreateTempTableEntityAsync(client, entity);
            await context.WhenTableEntityDeletedAsync(client, entity);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreTableEntityAsync(client, entity);
        }

        private async Task<TemporaryTableEntity> CreateTempTableEntityAsync(TableClient client, TableEntity entity)
        {
            return await TemporaryTableEntity.AddIfNotExistsAsync(client.AccountName, client.Name, entity, Logger);
        }

        private async Task<TableStorageTestContext> GivenTableStorageAsync()
        {
            return await TableStorageTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
