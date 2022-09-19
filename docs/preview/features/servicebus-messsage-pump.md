---
title: Testing Azure Service Bus message handling
layout: default
---

# Testing Azure Service Bus message handling

## Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Messaging.Pumps.ServiceBus
```

## Test Azure Service Bus message pump
As an addition on the [Arcus Azure Service Bus message pump](https://messaging.arcus-azure.net/Features/message-handling/service-bus), we have provided a test version of the message pump to verify your custom Azure Service Bus message handler implementations.
These hander implementations can be tested separately, and could be tested by interacting with the message router directly, but simulating messages like it would be from Azure Service Bus itself is a bit trickier.
This test message pump functionality allows you to verify certain cases without the need of an actual Azure resource.

We provide an extension that acts as an Azure Service Bus message pump and lets you decide how messages should be produced.

Consider the following message handler implementations to test:
```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

public class OrderAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    public async Task ProcessMessageAsync(
        Order message,
        AzureServiceBusMessageContext context,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        // Proces order...
    }
}

public class ShipmentAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
{
    public async Task ProcessMessageAsync(
        Shipment message,
        AzureServiceBusMessageContext context,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        // Proces order...
    }
}
```

Testing these together requires us to register them into an application. The following example shows how instead of calling `.AddServiceBusMessagePump(...)`, you can call the testing variant `.AddTestServiceBusMessagePump(...)`.
The extension allows you to pass in a 'message producer'. This producer allows you to control which kind of messages the test message pump should simulate. This example shows how a single `Order` message can be added to the message pump.

```csharp
using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class MessageHandlingTests
{
    private readonly ITestOutputWriter _outputWriter;

    public MessageHandlingTests(ITestOutputWriter outputWriter)
    {
        _outputWriter = outputWriter;
    }

    [Fact]
    public async Task RegisterOrderAndShipmentMessageHandler_PublishOrder_ProcessOrderCorrectly()
    {
        // Arrange
        var order = new Order(orderId: "123", scheduled: DateTimeOffset.UtcNow);
        var handler = new OrderAzureServiceBusMessageHandler();

        IHostBuilder builder =
            Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddXunitTestLogging(_outputWriter))
                .ConfigureServices(services =>
                {
                    services.AddTestServiceBusMessagePump(producer => producer.AddMessageBody(order))
                            .WithServiceBusMessageHandler<OrderAzureServiceBusMessageHandler, Order>(provider => handler)
                            .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>();
                });

        using (IHost host = builder.Build())
        {
            try
            {
                // Act
                host.StartAsync();

                // Assert
                Assert.True(handler.IsProcessed);
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}
```

Note that in this example, we use `Assert.True(handler.IsProcessed)` to determine if the `Order` message was correctly processed. In your application, you may want to inject your message handlers with test versions of dependencies and determine via those dependencies if the correct message handler was called.
You can use one of the `.WithServiceBusMessageHandler<,>(...)` extensions to pass in your instance of the message handler so you can use it later in the test assertion, like it is shown in the example.

> 💡 Note that the example uses `logging.AddXunitTestLogging`. This is available in the `Arcus.Testing.Logging` package. See [this page on logging](./logging.md) for more information.

## Message producer configuration
The test message pump can be configured extensively to meet your needs. You can even pass in your own implementation of a test message producer to have full control over how the Azure Service Bus messages should look like.
When a custom message body is passed, an `ServiceBusReceivedMessage` will be created with the [correlation properties](https://messaging.arcus-azure.net/Features/message-handling/service-bus#message-correlation) already filled out. This allows for you to also test any the correlation specific functionality in your message handlers.

```csharp
services.AddTestServiceBusMessagePump(producer =>
{
    // Pass in a single custom message, which will become a `ServiceBusReceivedMessage`.
    producer.AddMessageBody(order);

    // Pass in multiple custom messages, which will become `ServiceBusReceivedMessage` instances.
    producer.AddMessageBodies(orders);

    // Pass in your own `ServiceBusReceivedMessage`.
    // 💡 Use Microsoft's `ServiceBusModelFactory` to create such messages.
    var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
        body: BinaryData.FromObjectAsJson(shipment),
        messageId: Guid.NewGuid().ToString());
    producer.AddMessage(message);

    // Pass in your own `ServiceBusReceivedMessage` instances.
    // 💡 Use Microsoft's `ServiceBusModelFactory` to create such messages.
    var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
        body: BinaryData.FromObjectAsJson(shipment),
        messageId: Guid.NewGuid().ToString());
    producer.AddMessages(new[] { message });
});
```

If the creation of Azure Service Bus messages is rather complex, you require asynchronous serialization, or you want to re-use your message producing; you can implement your own message producer.
This requires you to implement the `IAzureServiceBusMessageProducer` interface.
```csharp
public class MyTestMessageProducer : IAzureServiceBusMessageProducer
{
    public async Task<ServiceBusReceivedMessage[]> ProduceMessagesAsync()
    {
        var order = new Order(orderId: "123", scheduled: DateTimeOffset.UtcNow);

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromObjectAsJson(shipment),
            messageId: Guid.NewGuid().ToString(),
            properties: new Dictionary<string, object>
            {
                ["X-My-Custom-Key"] = "My-Custom-Value"
            });

        return Task.FromResult(new[] { message });
    }
}
```

Such implementation can be pased along during the registration:
```csharp
var producer = new MyTestMessageProducer();
services.AddTestServiceBusMessagePump(producer);
```

## Message routing configuration
The test Azure Service Bus registration internally uses the Azure Service Bus message router. To configure this router, use one of the extension overloads. This gives you access to the message router options, like in a general Azure Service Bus message pump registration.
```csharp
services.AddTestServiceBusMessagePump(..., options =>
{
    options.Deserialization.AdditionalMembers = AdditionalMemberHandling.Error
});
```

For more information on the message router options, see the [Arcus messaging feature documentation](https://messaging.arcus-azure.net/Features/message-handling/service-bus#pump-configuration).
