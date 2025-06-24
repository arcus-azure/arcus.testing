using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Fixture;
using Bogus;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage
{
    [Collection(TestCollections.NoSql)]
    public class TemporaryNoSqlItemTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryNoSqlItemTests" /> class.
        /// </summary>
        public TemporaryNoSqlItemTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempNoSqlItem_WithNonExistingItem_SucceedsByDeletingAfterLifetimeFixture()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            Product expected = CreateProduct();
            string containerName = await context.WhenContainerNameAvailableAsync(expected.PartitionKeyPath);

            TemporaryNoSqlItem item = await WhenTempItemCreatedAsync(context, containerName, expected);
            await context.ShouldStoreItemAsync(containerName, expected, actual => AssertProduct(expected, actual));

            // Act
            await item.DisposeAsync();

            // Assert
            await context.ShouldNotStoreItemAsync(containerName, expected);
        }

        [Fact]
        public async Task CreateTempNoSqlItem_WithExistingItem_SucceedsByRevertingItemAfterLifetimeFixture()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            Product original = CreateProduct();
            Product newProduct = CreateProduct();
            newProduct.SetId(original);
            newProduct.SetPartitionKey(original);

            string containerName = await context.WhenContainerNameAvailableAsync(original.PartitionKeyPath);
            await context.WhenItemAvailableAsync(containerName, original);

            TemporaryNoSqlItem item = await WhenTempItemCreatedAsync(context, containerName, newProduct);
            await context.ShouldStoreItemAsync(containerName, original, actual => AssertProduct(newProduct, actual));

            // Act
            await item.DisposeAsync();

            // Assert
            await context.ShouldStoreItemAsync(containerName, original, actual => AssertProduct(original, actual));
        }

        public static IEnumerable<object[]> ItemsWithOtherPartitionKeyTypes => new[]
        {
            new object[] { ItemWithIntPartitionKey.Generate() },
            new object[] { ItemWithBoolPartitionKey.Generate() },
            new object[] { ItemWithNullPartitionKey.Generate() },
            new object[] { ItemWithNonePartitionKey.Generate() }
        };

        [Theory]
        [MemberData(nameof(ItemsWithOtherPartitionKeyTypes))]
        public async Task CreateTempNoSqlItem_WithOtherPartitionKeyType_SucceedsByFollowingSameStandard<T>(T item) where T : INoSqlItem
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            string containerName = await context.WhenContainerNameAvailableAsync(item.PartitionKeyPath);
            TemporaryNoSqlItem temp = await WhenTempItemCreatedAsync(context, containerName, item);

            await context.ShouldStoreItemAsync(containerName, item);

            // Act
            await temp.DisposeAsync();

            // Assert
            await context.ShouldNotStoreItemAsync(containerName, item);
        }

        [Fact]
        public async Task CreateTempNoSqlItem_WithoutId_FailsWithInvalidOperation()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            Product item = CreateProduct();
            item.Id = "";

            string containerName = await context.WhenContainerNameAvailableAsync(item.PartitionKeyPath);

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => WhenTempItemCreatedAsync(context, containerName, item));
        }

        public class ItemWithIntPartitionKey : INoSqlItem
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public int PartitionKey { get; set; }
            public static ItemWithIntPartitionKey Generate() => new() { Id = Bogus.Random.Guid().ToString(), PartitionKey = Bogus.Random.Int(1, 100) };
            public string GetId() => Id;
            public PartitionKey GetPartitionKey() => new(PartitionKey);
            public string PartitionKeyPath => "/" + nameof(PartitionKey);
        }

        public class ItemWithBoolPartitionKey : INoSqlItem
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public bool PartitionKey { get; set; }
            public static ItemWithBoolPartitionKey Generate() => new() { Id = Bogus.Random.Guid().ToString(), PartitionKey = Bogus.Random.Bool() };
            public string GetId() => Id;
            public PartitionKey GetPartitionKey() => new(PartitionKey);
            public string PartitionKeyPath => "/" + nameof(PartitionKey);
        }

        public class ItemWithNullPartitionKey : INoSqlItem
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string PartitionKey { get; set; }
            public static ItemWithNullPartitionKey Generate() => new() { Id = Bogus.Random.Guid().ToString(), PartitionKey = null };
            public string GetId() => Id;
            public PartitionKey GetPartitionKey() => new(null);
            public string PartitionKeyPath => "/" + nameof(PartitionKey);
        }

        public class ItemWithNonePartitionKey : INoSqlItem
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string PartitionKey { get; set; }
            public static ItemWithNonePartitionKey Generate() => new() { Id = Bogus.Random.Guid().ToString() };
            public string GetId() => Id;
            public PartitionKey GetPartitionKey() => Microsoft.Azure.Cosmos.PartitionKey.None;
            public string PartitionKeyPath => "/some";
        }

        private NoSqlTestContext GivenCosmosNoSql()
        {
            return NoSqlTestContext.Given(Configuration, Logger);
        }

        private static void AssertProduct(Product expected, Product actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Quantity, actual.Quantity);
            Assert.Equal(expected.Sale, actual.Sale);
            Assert.Equal(expected.Category, actual.Category);
        }

        private sealed class Product : INoSqlItem<Product>
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string Category { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public bool Sale { get; set; }

            public string GetId() => Id;
            public void SetId(Product p) { Id = p.Id; }
            public void SetPartitionKey(Product p) { Category = p.Category; }
            public PartitionKey GetPartitionKey() => new(Category);
            public string PartitionKeyPath => "/" + nameof(Category);
        }

        private static Product CreateProduct()
        {
            return new Faker<Product>()
                .RuleFor(p => p.Id, f => f.Random.Guid().ToString())
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Category, f => f.PickRandom(f.Commerce.Categories(10)))
                .RuleFor(p => p.Quantity, f => f.Random.Int(1, 100))
                .RuleFor(p => p.Sale, f => f.Random.Bool())
                .Generate();
        }

        [Fact]
        public async Task CreateTempNoSqlItem_WithAlreadyDeletedItem_SucceedsByIgnoringDeletion()
        {
            // Arrange
            await using NoSqlTestContext context = GivenCosmosNoSql();

            Product expected = CreateProduct();
            string containerName = await context.WhenContainerNameAvailableAsync(expected.PartitionKeyPath);

            TemporaryNoSqlItem item = await WhenTempItemCreatedAsync(context, containerName, expected);
            await context.WhenItemDeletedAsync(containerName, expected);

            // Act
            await item.DisposeAsync();

            // Assert
            await context.ShouldNotStoreItemAsync(containerName, expected);
        }

        private async Task<TemporaryNoSqlItem> WhenTempItemCreatedAsync<T>(NoSqlTestContext context, string containerName, T item, Container container = null)
            where T : INoSqlItem
        {
            container ??= context.Database.GetContainer(containerName);
#pragma warning disable CS0618 // Type or member is obsolete: currently still testing deprecated functionality.
            var temp = await TemporaryNoSqlItem.InsertIfNotExistsAsync(container, item, Logger);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(item.GetId(), temp.Id);
            Assert.Equal(item.GetPartitionKey(), temp.PartitionKey);

            return temp;
        }
    }
}
