using System;
using Bogus;
using Newtonsoft.Json;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class Order
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("product")]
        public Product Product { get; set; }

        [JsonProperty("scheduled")]
        public DateTimeOffset Scheduled { get; set; }

        public static Order Generate()
        {
            var productGenerator = new Faker<Product>()
                .RuleFor(p => p.ProductName, faker => faker.Commerce.ProductName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(o => o.OrderId, faker => faker.Random.Guid().ToString())
                .RuleFor(o => o.Scheduled, faker => faker.Date.RecentOffset())
                .RuleFor(o => o.Product, faker => productGenerator.Generate());

            return orderGenerator.Generate();
        }
    }
}