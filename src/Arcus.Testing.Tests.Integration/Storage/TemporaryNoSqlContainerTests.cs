using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Bogus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.NoSql)]
    public class TemporaryNoSqlContainerTests : IntegrationTest
    {
        private string[] PartitionKeyPaths => new[]
        {
            "/" + nameof(Ship.Destination) + "/" + nameof(Destination.Country)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryNoSqlContainerTests" /> class.
        /// </summary>
        public TemporaryNoSqlContainerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        private NoSqlConfig NoSql => Configuration.GetNoSql();

        [Fact]
        public async Task CreateTempNoSqlContainer_WithNonExistingContainer_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using NoSqlTestContext context = GivenNoSql();

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
            await using NoSqlTestContext context = GivenNoSql();

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
            await using NoSqlTestContext context = GivenNoSql();

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
            await using NoSqlTestContext context = GivenNoSql();

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
            await using NoSqlTestContext context = GivenNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdMatched = await context.WhenItemAvailableAsync(containerName, CreateShip());
            Ship createdNotMatched = await context.WhenItemAvailableAsync(containerName, CreateShip());

            // Act
            await WhenTempContainerCreatedAsync(containerName, options =>
            {
                options.OnSetup.CleanMatchingItems(CreateMatchingFilter(createdMatched))
                               .CleanMatchingItems(CreateMatchingFilter(createdMatched))
                               .CleanMatchingItems(CreateMatchingFilter(CreateShip()));
            });

            // Assert
            await context.ShouldStoreItemAsync(containerName, createdNotMatched);
            await context.ShouldNotStoreItemAsync(containerName, createdMatched);
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanMatchingOnTeardown_WhenExistingItem_SucceedsByCleaningSubset()
        {
            // Arrange
            await using NoSqlTestContext context = GivenNoSql();

            string containerName = await WhenContainerAlreadyAvailableAsync(context);
            Ship createdMatched = CreateShip();
            Ship createdNotMatched = CreateShip();

            TemporaryNoSqlContainer container = await WhenTempContainerCreatedAsync(containerName, options =>
            {
                options.OnTeardown.CleanMatchingItems(CreateMatchingFilter(createdMatched))
                                  .CleanMatchingItems(CreateMatchingFilter(createdMatched))
                                  .CleanMatchingItems(CreateMatchingFilter(CreateShip()));
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

        private static NoSqlItemFilter CreateMatchingFilter(Ship item)
        {
            return Bogus.Random.Int(1, 4) switch
            {
                1 => NoSqlItemFilter.ItemIdEqual(item.Id),
                2 => NoSqlItemFilter.ItemIdEqual(item.Id, StringComparison.OrdinalIgnoreCase),
                3 => NoSqlItemFilter.PartitionKeyEqual(item.GetPartitionKey()),
                4 => NoSqlItemFilter.ItemEqual<Ship>(x => x.BoatName == item.BoatName),
                _ => throw new ArgumentOutOfRangeException(nameof(item), "Unknown filter type")
            };
        }

        [Fact]
        public async Task CreateTempNoSqlContainerWithCleanAllOnTeardown_WhenExistingItem_SucceedsByCleaningAllItems()
        {
            // Arrange
            await using NoSqlTestContext context = GivenNoSql();

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

        private NoSqlTestContext GivenNoSql()
        {
            return NoSqlTestContext.Given(Configuration, Logger);
        }

        private async Task<string> WhenContainerAlreadyAvailableAsync(NoSqlTestContext context)
        {
            return await context.WhenContainerNameAvailableAsync(PartitionKeyPaths.Single());
        }

        private async Task<TemporaryNoSqlContainer> WhenTempContainerCreatedAsync(
            string containerName,
            Action<TemporaryNoSqlContainerOptions> configureOptions = null)
        {
            var container = 
                configureOptions is null
                    ? await TemporaryNoSqlContainer.CreateIfNotExistsAsync(NoSql.ResourceId, NoSql.DatabaseName, containerName, PartitionKeyPaths.Single(), Logger)
                    : await TemporaryNoSqlContainer.CreateIfNotExistsAsync(NoSql.ResourceId, NoSql.DatabaseName, containerName, PartitionKeyPaths.Single(), Logger, configureOptions);

            Assert.Equal(NoSql.DatabaseName, container.Client.Database.Id);
            Assert.Equal(containerName, container.Client.Id);

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
                .RuleFor(s => s.Destination, f => new Destination { Country = f.Address.Country() })
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
