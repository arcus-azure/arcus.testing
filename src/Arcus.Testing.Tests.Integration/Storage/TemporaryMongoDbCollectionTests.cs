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
    public class TemporaryMongoDbCollectionTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryMongoDbCollectionTests" /> class.
        /// </summary>
        public TemporaryMongoDbCollectionTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private MongoDbConfig MongoDb => Configuration.GetMongoDb();

        [Fact]
        public async Task CreateTempMongoDbCollection_OnNonExistingCollection_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = context.WhenCollectionNameUnavailable();
            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName);

            await context.ShouldStoreCollectionAsync(collectionName);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldNotStoreCollectionAsync(collectionName);
        }

        [Fact]
        public async Task CreateTempMongoDbCollection_OnExistingCollection_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            Shipment shipment = CreateShipment();
            BsonValue existingId = await context.WhenDocumentAvailableAsync(collectionName, shipment);

            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName);
            Shipment createdByUs = CreateShipment();
            await collection.AddDocumentAsync(createdByUs);

            await context.ShouldStoreCollectionAsync(collectionName);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, existingId);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, createdByUs.BoatId);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldStoreCollectionAsync(collectionName);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, existingId);
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, createdByUs.BoatId);
        }

        [Fact]
        public async Task CreateTempMongoDbCollectionWithCleanAllOnSetup_OnExistingCollectionWithExistingDocument_SucceedsByRemovingDocument()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();

            Shipment shipment = CreateShipment();
            BsonValue existingId = await context.WhenDocumentAvailableAsync(collectionName, shipment);

            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName, options =>
            {
                options.OnSetup.CleanAllDocuments();
            });

            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, existingId);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldStoreCollectionAsync(collectionName);
        }

        [Fact]
        public async Task CreateTempMongoDbCollectionWithCleanMatchingOnSetup_OnExistingCollectionWithMatchingDocument_SucceedsByRemovingDocument()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();

            Shipment matched = CreateShipment();
            Shipment unmatched = CreateShipment();
            BsonValue matchedId = await context.WhenDocumentAvailableAsync(collectionName, matched);
            BsonValue unmatchedId = await context.WhenDocumentAvailableAsync(collectionName, unmatched);

            // Act
            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName, options =>
            {
                options.OnSetup.CleanMatchingDocuments((Shipment s) => s.BoatId == (ObjectId) matchedId)
                               .CleanMatchingDocuments((Shipment s) => s.BoatName == matched.BoatName);
            });

            // Assert
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, matchedId);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, unmatchedId);
            
            await collection.DisposeAsync();
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, unmatchedId);
        }

        [Fact]
        public async Task CreateTempMongoDbCollectionWithCleanAllOnTeardown_OnExistingCollectionWithNewDocument_SucceedsByRemovingDocument()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();
            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName, options =>
            {
                options.OnTeardown.CleanAllDocuments();
            });

            Shipment afterwards = CreateShipment();
            BsonValue afterwardsId = await context.WhenDocumentAvailableAsync(collectionName, afterwards);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, afterwardsId);
        }

        [Fact]
        public async Task CreateTempMongoDbCollectionWithCleanMatchingOnTeardown_OnExistingCollectionWithNewMatchingDocument_SucceedsByRemovingDocument()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();

            Shipment matched = CreateShipment();
            Shipment unmatched = CreateShipment();
            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName, options =>
            {
                options.OnTeardown.CleanMatchingDocuments((Shipment s) => s.BoatName != unmatched.BoatName)
                                  .CleanMatchingDocuments((Shipment s) => s.BoatName == matched.BoatName);
            });

            BsonValue matchedId = await context.WhenDocumentAvailableAsync(collectionName, matched);
            BsonValue unmatchedId = await context.WhenDocumentAvailableAsync(collectionName, unmatched);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, matchedId);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, unmatchedId);
        }

        [Fact]
        public async Task CreateTempMongoDbCollectionWithSetupTeardown_OnExistingCollection_SucceedsByPartiallyDeletingDocuments()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = await context.WhenCollectionNameAvailableAsync();

            Shipment matchedOnSetup = CreateShipment();
            Shipment unmatchedOnSetup = CreateShipment();
            Shipment createdByUs = CreateShipment();
            BsonValue matchedOnSetupId = await context.WhenDocumentAvailableAsync(collectionName, matchedOnSetup);
            BsonValue unmatchedOnSetupId = await context.WhenDocumentAvailableAsync(collectionName, unmatchedOnSetup);

            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName, options =>
            {
                options.OnSetup.CleanMatchingDocuments((Shipment s) => s.BoatName == matchedOnSetup.BoatName);
            });
            collection.OnTeardown.CleanAllDocuments();
            await collection.AddDocumentAsync(createdByUs);

            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, matchedOnSetupId);
            await context.ShouldStoreDocumentAsync<Shipment>(collectionName, unmatchedOnSetupId);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, unmatchedOnSetupId);
            await context.ShouldNotStoreDocumentAsync<Shipment>(collectionName, createdByUs.BoatId);
        }

        private static Shipment CreateShipment()
        {
            return new Faker<Shipment>()
                .RuleFor(s => s.BoatId, _ => ObjectId.GenerateNewId())
                .RuleFor(s => s.BoatName, f => f.Person.FirstName)
                .Generate();
        }

        public class Shipment
        {
            [BsonId]
            public ObjectId BoatId { get; set; }

            public string BoatName { get; set; }
        }

        [Fact]
        public async Task CreateTempMongoDbCollection_WhenCollectionWasDeletedOutsideFixture_SucceedsByIgnoringDeletion()
        {
            // Arrange
            await using MongoDbTestContext context = await GivenCosmosMongoDbAsync();

            string collectionName = context.WhenCollectionNameUnavailable();
            TemporaryMongoDbCollection collection = await WhenTempCollectionCreatedAsync(collectionName);
            await context.WhenCollectionDeletedAsync(collectionName);

            // Act
            await collection.DisposeAsync();

            // Assert
            await context.ShouldNotStoreCollectionAsync(collectionName);
        }

        private async Task<TemporaryMongoDbCollection> WhenTempCollectionCreatedAsync(string collectionName, Action<TemporaryMongoDbCollectionOptions> configureOptions = null)
        {
            var collection = configureOptions is null
                ? await TemporaryMongoDbCollection.CreateIfNotExistsAsync(MongoDb.ResourceId, MongoDb.DatabaseName, collectionName, Logger)
                : await TemporaryMongoDbCollection.CreateIfNotExistsAsync(MongoDb.ResourceId, MongoDb.DatabaseName, collectionName, Logger, configureOptions);

            Assert.Equal(collectionName, collection.Name);
            return collection;
        }

        private async Task<MongoDbTestContext> GivenCosmosMongoDbAsync()
        {
            return await MongoDbTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
