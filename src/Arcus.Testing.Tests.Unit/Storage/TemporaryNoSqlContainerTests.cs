using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Arcus.Testing.TemporaryNoSqlContainer;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryNoSqlContainerTests
    {
        private static ResourceIdentifier CosmosDbResourceId => ResourceIdentifier.Parse(
            $"/subscriptions/{Guid.NewGuid()}/resourceGroups/group/providers/Microsoft.DocumentDB/databaseAccounts/account");

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutDatabaseName_Fails(string databaseName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, databaseName, "<container-name>", "<partition-key-path>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, databaseName, "<container-name>", "<partition-key-path>", NullLogger.Instance, configureOptions: opt => { }));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), databaseName, "<container-name>", "<partition-key-path>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), databaseName, "<container-name>", "<partition-key-path>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutContainerName_Fails(string containerName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", containerName, "<partition-key-path>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", containerName, "<partition-key-path>", NullLogger.Instance, configureOptions: opt => { }));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), "<database-name>", containerName, "<partition-key-path>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), "<database-name>", containerName, "<partition-key-path>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempNoSqlContainer_WithoutPartitionKeyPath_Fails(string partitionKeyPath)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", "<container-name>", partitionKeyPath, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", "<container-name>", partitionKeyPath, NullLogger.Instance, configureOptions: opt => { }));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), "<database-name>", "<container-name>", partitionKeyPath, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, new DefaultAzureCredential(), "<database-name>", "<container-name>", partitionKeyPath, NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}
