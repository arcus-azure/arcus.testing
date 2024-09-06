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
    [Collection(TestCollections.MongoDb)]
    public class TemporaryMongoDbDocumentTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbDocumentTests" /> class.
        /// </summary>
        public TemporaryMongoDbDocumentTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private MongoDbConfig MongoDb => Configuration.GetMongoDb();

        [Fact]
        public async Task CreateTempMongoDbDocument_OnNonExistingDocumentId_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenMongoDbAsync();

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
            await using MongoDbTestContext context = await GivenMongoDbAsync();

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
            return await TemporaryMongoDbDocument.InsertIfNotExistsAsync(
                MongoDb.ResourceId,
                MongoDb.DatabaseName,
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

        private async Task<MongoDbTestContext> GivenMongoDbAsync()
        {
            return await MongoDbTestContext.GivenAsync(Configuration, Logger);
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithOtherThanObjectIdPropertyType_SucceedsByUsingBuiltInIdGenerator()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            var original = DocWithIntId.Generate();
            BsonValue id = await context.WhenDocumentAvailableAsync(collectionName, original);

            var replacement = DocWithIntId.Generate();
            replacement.Id = (Guid) id;

            TemporaryMongoDbDocument temp = await WhenTempDocumentCreatedAsync(collectionName, replacement);
            await context.ShouldStoreDocumentAsync<DocWithIntId>(collectionName, id, stored => Assert.Equal(replacement.Name, stored.Name));

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldStoreDocumentAsync<DocWithIntId>(collectionName, id, stored => Assert.Equal(original.Name, stored.Name));
        }

        public class DocWithIntId
        {
            [BsonId]
            public Guid Id { get; set; }

            public string Name { get; set; }

            public static DocWithIntId Generate()
            {
                return new DocWithIntId { Name = Bogus.Random.Word() };
            }
        }

        [Fact]
        public async Task CreateTempMongoDbDocument_WithoutIdProperty_Fails()
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => WhenTempDocumentCreatedAsync("<collection-name>", new DocWithoutId()));
        }

        public class DocWithoutId { }
    }
}
