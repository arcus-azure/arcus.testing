using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Bogus;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.CosmosDb)]
    public class TemporaryMongoDbDocumentTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbDocumentTests" /> class.
        /// </summary>
        public TemporaryMongoDbDocumentTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private CosmosDbConfig CosmosDb => Configuration.GetCosmosDb();

        [Fact]
        public async Task CreateTempMongoDbDocument_OnNonExistingDocumentId_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using CosmosDbTestContext context = await GivenMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            Product product = CreateProduct();

            TemporaryMongoDbDocument temp = await WhenTempDocumentCreatedAsync(collectionName, product);
            var id = ObjectId.Parse(temp.Id);
            await context.ShouldStoreDocumentAsync<Product>(collectionName, id, stored => AssertProduct(product, stored));

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreDocumentAsync<Product>(collectionName, id);
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_OnExistingDocumentId_SucceedsByRevertingAfterFixtureLifetime()
        {
            // Arrange
            await using CosmosDbTestContext context = await GivenMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            Product original = CreateProduct();
            ObjectId id = await context.WhenDocumentAvailableAsync(collectionName, original);

            Product replacement = CreateProduct();
            replacement.Id = id;

            TemporaryMongoDbDocument temp = await WhenTempDocumentCreatedAsync(collectionName, replacement);
            await context.ShouldStoreDocumentAsync<Product>(collectionName, id, stored => AssertProduct(replacement, stored));

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldStoreDocumentAsync<Product>(collectionName, id, stored => AssertProduct(original, stored));
        }

        private async Task<TemporaryMongoDbDocument> WhenTempDocumentCreatedAsync<TDoc>(string collectionName, TDoc doc) 
            where TDoc : class
        {
            return await TemporaryMongoDbDocument.InsertIfNotExistsAsync(
                CosmosDb.ResourceId,
                CosmosDb.MongoDb.DatabaseName,
                collectionName,
                doc,
                Logger);
        }

        public class Product
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
            public int Amount { get; set; }
        }

        private static Product CreateProduct()
        {
            return new Faker<Product>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName() + f.Random.Guid())
                .RuleFor(p => p.Amount, f => f.Random.Int(1, 100))
                .Generate();
        }

        private static void AssertProduct(Product expected, Product actual)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Amount, actual.Amount);
        }

        private async Task<CosmosDbTestContext> GivenMongoDbAsync()
        {
            return await CosmosDbTestContext.GivenMongoDbAsync(Configuration, Logger);
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithUnsupportedIdPropertyType_Fails()
        {
            await Assert.ThrowsAnyAsync<NotSupportedException>(() => WhenTempDocumentCreatedAsync("<collection-name>", new DocWithUnsupportedId()));
        }

        public class DocWithUnsupportedId
        {
            [BsonId]
            [BsonRepresentation(BsonType.Int32)]
            public int Id { get; set; }
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithoutIdProperty_Fails()
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => WhenTempDocumentCreatedAsync("<collection-name>", new DocWithoutId()));
        }

        public class DocWithoutId { }
    }
}
