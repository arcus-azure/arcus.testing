using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryNoSqlContainerTests
    {
        private ResourceIdentifier CosmosDbResourceId => ResourceIdentifier.Parse(
            $"/subscriptions/{Guid.NewGuid()}/resourceGroups/group/providers/Microsoft.DocumentDB/databaseAccounts/account");

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutDatabaseName_Fails(string databaseName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryNoSqlContainer.CreateIfNotExistsAsync(
                    CosmosDbResourceId,
                    databaseName,
                    "<container-name>",
                    "<partition-key-path>",
                    NullLogger.Instance));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutContainerName_Fails(string containerName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryNoSqlContainer.CreateIfNotExistsAsync(
                    CosmosDbResourceId,
                    "<database-name>",
                    containerName,
                    "<partition-key-path>",
                    NullLogger.Instance));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutPartitionKeyPath_Fails(string partitionKeyPath)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryNoSqlContainer.CreateIfNotExistsAsync(
                    CosmosDbResourceId,
                    "<database-name>",
                    "<container-name>",
                    partitionKeyPath,
                    NullLogger.Instance));
        }
    }
}
