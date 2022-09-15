using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Bogus;
using Newtonsoft.Json;
using Xunit;

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

    public class Product
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }
    }

    public class Shipment
    {
        [JsonProperty("container")]
        public Container Container { get; set; }

        [JsonProperty("arrived")]
        public DateTimeOffset Arrived { get; set; }

        public static Shipment Generate()
        {
            var containerGenerator = new Faker<Container>()
                .RuleFor(c => c.SerialNumber, faker => faker.Random.AlphaNumeric(12));

            var shipmentGenerator = new Faker<Shipment>()
                .RuleFor(s => s.Container, faker => containerGenerator.Generate())
                .RuleFor(s => s.Arrived, faker => faker.Date.RecentOffset());

            return shipmentGenerator.Generate();
        }
    }

    public class Container
    {
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }
    }

    public class OrderAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly Order[] _expected;
        private int _expectedCount;

        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public OrderAzureServiceBusMessageHandler(params Order[] expected)
        {
            _expected = expected;
            _expectedCount = 0;
        }

        public Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Assert.Single(_expected, expected =>
            {
                return message != null
                       && message.OrderId == expected.OrderId
                       && message.Scheduled == expected.Scheduled
                       && message.Product?.ProductName == expected.Product.ProductName;
            });
            IsProcessed = ++_expectedCount == _expected.Length;
            
            return Task.CompletedTask;
        }
    }

    public class ShipmentAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
    {
        private readonly Shipment[] _expected;
        private int _expectedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public ShipmentAzureServiceBusMessageHandler()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public ShipmentAzureServiceBusMessageHandler(params Shipment[] expected)
        {
            _expected = expected;
        }

        public bool IsProcessed { get; private set; }

        public Task ProcessMessageAsync(
            Shipment message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Assert.Single(_expected, expected =>
            {
                return message != null
                       && message.Arrived == expected.Arrived
                       && message.Container?.SerialNumber == expected.Container.SerialNumber;
            });
            IsProcessed = ++_expectedCount == _expected.Length;

            return Task.CompletedTask;
        }
    }
}
