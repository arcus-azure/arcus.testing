using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using Xunit;
using static Arcus.Testing.TemporaryMongoDbCollection;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryMongoDbCollectionTests
    {
        private ResourceIdentifier CosmosDbResourceId => ResourceIdentifier.Parse(
            $"/subscriptions/{Guid.NewGuid()}/resourceGroups/group/providers/Microsoft.DocumentDB/databaseAccounts/account");

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempCollectionViaResourceId_WithoutDatabaseName_Fails(string databaseName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, databaseName, "<collection-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, databaseName, "<collection-name>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempCollectionViaResourceId_WithoutCollectionName_Fails(string collectionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", collectionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(CosmosDbResourceId, "<database-name>", collectionName, NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempCollectionViaClient_WithoutCollectionName_Fails(string collectionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(Mock.Of<IMongoDatabase>(), collectionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateIfNotExistsAsync(Mock.Of<IMongoDatabase>(), collectionName, NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}