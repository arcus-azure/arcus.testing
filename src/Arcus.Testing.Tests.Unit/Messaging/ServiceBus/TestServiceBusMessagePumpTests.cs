using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus
{
    public class TestServiceBusMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessagePumpTests" /> class.
        /// </summary>
        public TestServiceBusMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task TestMessagePump_WithSingleMessageBody_ProcessesCorrectly()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessageBody(order))
                                    .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownSingleMessageBody_DoesNotProcesses()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);
            var shipment = Shipment.Generate();

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessageBody(shipment))
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithMultipleMessageBodies_ProcessesCorrectly()
        {
            // Arrange
            IEnumerable<Order> orders = BogusGenerator.Make(10, Order.Generate);
            var handler = new OrderAzureServiceBusMessageHandler(orders.ToArray());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessageBodies(orders))
                                    .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownMultipleMessageBodies_DoesNotProcesses()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);
            IEnumerable<Shipment> shipments = BogusGenerator.Make(10, Shipment.Generate);

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessageBodies(shipments))
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithSingleMessage_ProcessesCorrectly()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);
            ServiceBusReceivedMessage message = 
                ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: BinaryData.FromObjectAsJson(order),
                    messageId: Guid.NewGuid().ToString());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessage(message))
                                    .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownSingleMessage_DoesNotProcesses()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);
            var shipment = Shipment.Generate();
            ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromObjectAsJson(shipment),
                messageId: Guid.NewGuid().ToString());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessage(message))
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithMultipleMessages_ProcessesCorrectly()
        {
            // Arrange
            IEnumerable<Order> orders = BogusGenerator.Make(10, Order.Generate);
            var handler = new OrderAzureServiceBusMessageHandler(orders.ToArray());
            IEnumerable<ServiceBusReceivedMessage> messages = orders.Select(order =>
            {
                return ServiceBusModelFactory.ServiceBusReceivedMessage(
                        body: BinaryData.FromObjectAsJson(order),
                        messageId: Guid.NewGuid().ToString());
            });

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessages(messages))
                                    .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownMultipleMessages_DoesNotProcesses()
        {
            // Arrange
            var order = Order.Generate();
            var handler = new OrderAzureServiceBusMessageHandler(order);
            IEnumerable<Shipment> shipments = BogusGenerator.Make(10, Shipment.Generate);
            IEnumerable<ServiceBusReceivedMessage> messages = shipments.Select(shipment =>
            {
                return ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: BinaryData.FromObjectAsJson(shipment),
                    messageId: Guid.NewGuid().ToString());
            });

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestServiceBusMessagePump(produce => produce.AddMessages(messages))
                                    .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        private async Task StartNewHostAsync(
            Action<IServiceCollection> configureServices,
            Action test)
        {
            IHostBuilder builder =
                Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging => logging.AddXunitTestLogging(_outputWriter))
                    .ConfigureServices(configureServices);

            using (IHost host = builder.Build())
            {
                try
                {
                    await host.StartAsync();
                    test();
                }
                finally
                {
                    await host.StopAsync();
                }
            }
        }
    }
}
