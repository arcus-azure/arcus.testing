using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryMongoDbDocumentTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbDocumentTests" /> class.
        /// </summary>
        public TemporaryMongoDbDocumentTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempMongoDbDocument_WithoutDatabaseName_Fails(string databaseName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument<SampleDoc>.InsertIfNotExistsAsync(ResourceIdentifier.Root, databaseName, "<collection-name>", new SampleDoc(), _logger));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempMongoDbDocument_WithoutCollectionName_Fails(string collectionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument<SampleDoc>.InsertIfNotExistsAsync(ResourceIdentifier.Root, "<database-name>", collectionName, new SampleDoc(), _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithoutCollection_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument<SampleDoc>.InsertIfNotExistsAsync(collection: null, new SampleDoc(), _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocumentViaCollection_WithoutDocument_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument<SampleDoc>.InsertIfNotExistsAsync(collection: Mock.Of<IMongoCollection<SampleDoc>>() , document: null, _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocumentViaResourceId_WithoutDocument_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument<SampleDoc>.InsertIfNotExistsAsync(ResourceIdentifier.Root, "<database-name>", "<collection-name>" , document: null, _logger));
        }

        public class SampleDoc
        {
            public ObjectId Id { get; set; }
        }
    }
}
