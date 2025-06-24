using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete: currently still testing deprecated functionality.

namespace Arcus.Testing.Tests.Unit.Storage
{
    extern alias ArcusXunitV3;

    public class TemporaryMongoDbDocumentTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbDocumentTests" /> class.
        /// </summary>
        public TemporaryMongoDbDocumentTests(ITestOutputHelper outputWriter)
        {
            _logger = new ArcusXunitV3::Arcus.Testing.XunitTestLogger(outputWriter);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempMongoDbDocument_WithoutDatabaseName_Fails(string databaseName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument.InsertIfNotExistsAsync(ResourceIdentifier.Root, databaseName, "<collection-name>", new SampleDoc(), _logger));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempMongoDbDocument_WithoutCollectionName_Fails(string collectionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument.InsertIfNotExistsAsync(ResourceIdentifier.Root, "<database-name>", collectionName, new SampleDoc(), _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithoutCollection_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument.InsertIfNotExistsAsync(collection: null, new SampleDoc(), _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocumentViaCollection_WithoutDocument_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument.InsertIfNotExistsAsync(collection: Mock.Of<IMongoCollection<SampleDoc>>(), document: null, _logger));
        }

        [Fact]
        public async Task CreateTempMongoDbDocumentViaResourceId_WithoutDocument_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryMongoDbDocument.InsertIfNotExistsAsync(ResourceIdentifier.Root, "<database-name>", "<collection-name>", document: (SampleDoc) null, _logger));
        }

        public class SampleDoc
        {
            public ObjectId Id { get; set; }
        }
    }
}
