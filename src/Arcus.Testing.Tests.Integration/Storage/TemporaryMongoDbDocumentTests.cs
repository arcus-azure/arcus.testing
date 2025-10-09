using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Bogus;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryMongoDbDocumentTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbDocumentTests" /> class.
        /// </summary>
        public TemporaryMongoDbDocumentTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private CosmosDbConfig MongoDb => Configuration.GetMongoDb();

        [Fact]
        public async Task CreateTempMongoDbDocument_OnNonExistingDocumentId_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            Product product = CreateProduct();

            TemporaryMongoDbDocument temp = await WhenTempDocumentCreatedAsync(collectionName, product);
            BsonValue id = temp.Id;
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
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            Product original = CreateProduct();
            BsonValue id = await context.WhenDocumentAvailableAsync(collectionName, original);

            Product replacement = CreateProduct();
            replacement.Id = (ObjectId) id;

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
#pragma warning disable CS0618 // Type or member is obsolete: currently still testing deprecated functionality.
            return await TemporaryMongoDbDocument.InsertIfNotExistsAsync(
                MongoDb.AccountResourceId,
                MongoDb.DatabaseName,
                collectionName,
                doc,
                Logger);
#pragma warning restore CS0618 // Type or member is obsolete
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

        private async Task<MongoDbTestContext> GivenCosmosMongoDbAsync()
        {
            return await MongoDbTestContext.GivenAsync(Configuration, Logger);
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithOtherThanObjectIdPropertyType_SucceedsByUsingBuiltInIdGenerator()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            var original = DocWithStringId.Generate();
            BsonValue id = await context.WhenDocumentAvailableAsync(collectionName, original);

            var replacement = DocWithStringId.Generate();
            replacement.Id = (string) id;

            TemporaryMongoDbDocument temp = await WhenTempDocumentCreatedAsync(collectionName, replacement);
            await context.ShouldStoreDocumentAsync<DocWithStringId>(collectionName, id, stored => Assert.Equal(replacement.Name, stored.Name));

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldStoreDocumentAsync<DocWithStringId>(collectionName, id, stored => Assert.Equal(original.Name, stored.Name));
        }

        public class DocWithStringId
        {
            [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
            public string Id { get; set; }

            public string Name { get; set; }

            public static DocWithStringId Generate()
            {
                return new DocWithStringId { Name = Bogus.Random.Word() };
            }
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithAlreadyDeletedDocument_SucceedsByIgnoringDeletion()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            Product expected = CreateProduct();
            string collectionName = await context.WhenCollectionNameAvailableAsync();

            TemporaryMongoDbDocument doc = await WhenTempDocumentCreatedAsync(collectionName, expected);
            BsonValue id = doc.Id;
            await context.WhenDocumentDeletedAsync<Product>(collectionName, id);

            // Act
            await doc.DisposeAsync();

            // Assert
            await context.ShouldNotStoreDocumentAsync<Product>(collectionName, id);
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithoutIdProperty_Fails()
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => WhenTempDocumentCreatedAsync("<collection-name>", new DocWithoutId()));
        }

#pragma warning disable S2094 // S2094: Types should not be empty - this is a test class to verify that the temporary document creation fails when no ID is present
        public class DocWithoutId { }
#pragma warning restore S2094
    }
}
