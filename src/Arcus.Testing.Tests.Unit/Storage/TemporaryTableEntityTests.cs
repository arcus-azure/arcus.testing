using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete: currently still testing deprecated functionality.

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryTableEntityTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTableEntity_WithoutAccountName_Fails(string accountName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTableEntity.AddIfNotExistsAsync(accountName, "tableName", new TableEntity(), NullLogger.Instance));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTableEntity_WithoutTableName_Fails(string tableName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTableEntity.AddIfNotExistsAsync("accountName", tableName, new TableEntity(), NullLogger.Instance));
        }

        [Fact]
        public async Task CreateTempTableEntity_WithoutClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTableEntity.AddIfNotExistsAsync(client: null, new TableEntity(), NullLogger.Instance));
        }

        [Fact]
        public async Task CreateTempTableEntity_WithoutEntity_Fails()
        {
            // Arrange
            var client = new TableClient(new Uri("https://table-endpoint"), "tableName", new DefaultAzureCredential());

            // Act / Assert
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTableEntity.AddIfNotExistsAsync<TableEntity>(client, entity: null, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTableEntity.AddIfNotExistsAsync<TableEntity>("accountName", "tableName", entity: null, NullLogger.Instance));
        }
    }
}
