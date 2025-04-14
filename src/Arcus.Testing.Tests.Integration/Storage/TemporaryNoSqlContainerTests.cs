using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Bogus;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.NoSql)]
    public class TemporaryNoSqlContainerTests : IntegrationTest
    {
        private static string[] PartitionKeyPaths => new[]
        {
            "/" + nameof(Ship.Destination) + "/" + nameof(Destination.Country)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryNoSqlContainerTests" /> class.
        /// </summary>
        public TemporaryNoSqlContainerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private CosmosDbConfig NoSql => Configuration.GetNoSql();

        [Fact]
        public async Task CreateTempNoSqlContainer_WithNonExistingContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerId = context.WhenContainerNameUnavailable();
            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerId);
            await context.ShouldStoreContainerAsync(containerId);

            Ship createdByUs = await AddItemAsync(container);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldNotStoreItemAsync(containerId, createdByUs);
            await context.ShouldNotStoreContainerAsync(containerId);
        }

        [Fact]
        public async Task CreateTempNoSqlContainer_WithExistingContainer_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdBefore = await context.WhenItemAvailableAsync(containerName, CreateShip());
            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerName);

            Ship createdByUs = await AddItemAsync(container);
            Ship createdAfter = await context.WhenItemAvailableAsync(containerName, CreateShip());

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldNotStoreItemAsync(containerName, createdByUs);

            await context.ShouldStoreItemAsync(containerName, createdBefore);
            await context.ShouldStoreItemAsync(containerName, createdAfter);
            await context.ShouldStoreContainerAsync(containerName);
        }

        [Fact]
        public async Task CreateTempNoSqlContainer_WhenContainerWasDeletedOutsideFixture_SucceedsByIgnoringDeletion()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = context.WhenContainerNameUnavailable();
            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerName);
            await context.WhenContainerDeletedAsync(containerName);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldNotStoreContainerAsync(containerName);
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanAllOnSetup_WhenExistingItem_SucceedsByCleaningAllItems()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdBefore = await context.WhenItemAvailableAsync(containerName, CreateShip());

            // Act
            await WhenTempContainerCreatedAsync(containerName, options =>
            {
                options.OnSetup.CleanAllItems();
            });

            // Assert
            await context.ShouldNotStoreItemAsync(containerName, createdBefore);
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanMatchingOnSetup_WhenExistingItem_SucceedsByCleaningSubset()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdMatched = await context.WhenItemAvailableAsync(containerName, CreateShip());
            Ship createdNotMatched = await context.WhenItemAvailableAsync(containerName, CreateShip());

            // Act
            await WhenTempContainerCreatedAsync(containerName, options =>
            {
                options.OnSetup.CleanMatchingItems(item => item.Id == createdMatched.Id)
                               .CleanMatchingItems((Ship item) => item.GetPartitionKey() == createdMatched.GetPartitionKey());
            });

            // Assert
            await context.ShouldStoreItemAsync(containerName, createdNotMatched);
            await context.ShouldNotStoreItemAsync(containerName, createdMatched);
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanMatchingOnTeardown_WhenExistingItem_SucceedsByCleaningSubset()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdMatched = CreateShip();
            Ship createdNotMatched = CreateShip();

            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerName, options =>
            {
                options.OnTeardown.CleanMatchingItems(item => item.PartitionKey == createdMatched.GetPartitionKey())
                                  .CleanMatchingItems((Ship item) => item.GetPartitionKey() == createdMatched.GetPartitionKey());
            });

            Ship createdByUs = await AddItemAsync(container);
            await context.WhenItemAvailableAsync(containerName, createdMatched);
            await context.WhenItemAvailableAsync(containerName, createdNotMatched);

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldStoreContainerAsync(containerName);
            await context.ShouldNotStoreItemAsync(containerName, createdByUs);
            await context.ShouldNotStoreItemAsync(containerName, createdMatched);
            await context.ShouldStoreItemAsync(containerName, createdNotMatched);
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanAllOnTeardown_WhenExistingItem_SucceedsByCleaningAllItems()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdBefore = await context.WhenItemAvailableAsync(containerName, CreateShip("before"));

            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerName);
            container.OnTeardown.CleanAllItems();
            await context.ShouldStoreItemAsync(containerName, createdBefore);

            Ship createdByUs = await AddItemAsync(container);
            Ship createdAfter = await context.WhenItemAvailableAsync(containerName, CreateShip("after"));

            // Act
            await container.DisposeAsync();

            // Assert
            await context.ShouldStoreContainerAsync(containerName);

            await context.ShouldNotStoreItemAsync(containerName, createdBefore);
            await context.ShouldNotStoreItemAsync(containerName, createdByUs);
            await context.ShouldNotStoreItemAsync(containerName, createdAfter);
        }

        private NoSqlTestContext GivenCosmosNoSql()
        {
            return NoSqlTestContext.Given(Configuration, Logger);
        }

        private static async Task<string> WhenContainerAlreadyAvailableAsync(NoSqlTestContext context)
        {
            return await context.WhenContainerNameAvailableAsync(PartitionKeyPaths.Single());
        }

        private async Task<TemporaryNoSqlContainer> WhenTempContainerCreatedAsync(
            string containerName,
            Action<TemporaryNoSqlContainerOptions> configureOptions = null)
        {
            var container =
                configureOptions is null
                    ? await TemporaryNoSqlContainer.CreateIfNotExistsAsync(NoSql.AccountResourceId, NoSql.DatabaseName, containerName, PartitionKeyPaths.Single(), Logger)
                    : await TemporaryNoSqlContainer.CreateIfNotExistsAsync(NoSql.AccountResourceId, NoSql.DatabaseName, containerName, PartitionKeyPaths.Single(), Logger, configureOptions);

            Assert.Equal(NoSql.DatabaseName, container.Client.Database.Id);
            Assert.Equal(containerName, container.Name);

            return container;
        }

        private static async Task<Ship> AddItemAsync(TemporaryNoSqlContainer container)
        {
            Ship item = CreateShip("own");
            await container.AddItemAsync(item);

            return item;
        }

        private static Ship CreateShip(string header = "")
        {
            return new Faker<Ship>()
                .RuleFor(s => s.Id, f => header + "item-" + f.Random.Guid())
                .RuleFor(s => s.BoatName, f => f.Person.FirstName)
                .RuleFor(s => s.CrewMembers, f => f.Random.Int(1, 10))
                .RuleFor(s => s.Destination, f => new Destination { Country = f.Random.Guid() + f.Address.Country() })
                .Generate();
        }

        public class Ship : INoSqlItem<Ship>
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string BoatName { get; set; }
            public int CrewMembers { get; set; }
            public Destination Destination { get; set; }
            public string GetId() => Id;
            public void SetId(Ship item) => Id = item.Id;
            public PartitionKey GetPartitionKey() => new PartitionKeyBuilder().Add(Destination.Country).Build();
            public string PartitionKeyPath => "/" + nameof(Destination) + "/" + nameof(Destination.Country);
            public void SetPartitionKey(Ship item) => Destination.Country = item.Destination.Country;
        }

        public class Destination
        {
            public string Country { get; set; }
        }
    }
}
