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
            await using NoSqlTestContext context = GivenNoSql();

            Product expected = CreateProduct();
            string containerName = await context.WhenContainerNameAvailableAsync(expected.GetPartitionKeyPath());

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
            await using NoSqlTestContext context = GivenNoSql();

            Product original = CreateProduct();
            Product newProduct = CreateProduct();
            newProduct.SetId(original);
            newProduct.SetPartitionKey(original);

            string containerName = await context.WhenContainerNameAvailableAsync(original.GetPartitionKeyPath());
            await context.WhenItemAvailableAsync(containerName, original);

            TemporaryNoSqlItem item = await WhenTempItemCreatedAsync(context, containerName, newProduct);
            await context.ShouldStoreItemAsync(containerName, original, actual => AssertProduct(newProduct, actual));

            // Act
            await item.DisposeAsync();

            // Assert
            await context.ShouldStoreItemAsync(containerName, original, actual => AssertProduct(original, actual));
        }

        private NoSqlTestContext GivenNoSql()
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

        private class Product : INoSqlItem<Product>
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
            public string GetPartitionKeyPath() => "/" + nameof(Category);
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

        private async Task<TemporaryNoSqlItem> WhenTempItemCreatedAsync<T>(NoSqlTestContext context, string containerName, T item)
            where T : INoSqlItem<T>
        {
            Container container = context.Database.GetContainer(containerName);
            return await TemporaryNoSqlItem.InsertIfNotExistsAsync(container, item, Logger);
        }
    }
}
