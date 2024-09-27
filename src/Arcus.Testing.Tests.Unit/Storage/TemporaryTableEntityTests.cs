using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryTableEntityTests
    {
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
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTableEntity.AddIfNotExistsAsync<TableEntity>(client, entity: null, NullLogger.Instance));
        }
    }
}
