---
title: Testing Azure EventHubs message handling
layout: default
---

# Testing Azure EventHubs message handling

## Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Messaging.Pumps.EventHubs
```

## Test Azure EventHubs message pump
As an addition on the [Arcus Azure EventHubs message pump](https://messaging.arcus-azure.net/Features/message-handling/event-hubs), we have provided a test version of the message pump to verify your custom Azure EventHubs message handler implementations.
These hander implementations can be tested separately, and could be tested by interacting with the message router directly, but simulating messages like it would be from Azure EventHubs itself is a bit trickier.
This test message pump functionality allows you to verify certain cases without the need of an actual Azure resource.

We provide an extension that acts as an Azure EventHubs message pump and lets you decide how messages should be produced.

Consider the following message handler implementations to test:
```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;

public class SensorReadingAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
{
    public async Task ProcessMessageAsync(
        SensorReading reading,
        AzureEventHubsMessageContext context,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        // Proces sensor reading...
    }
}

public class SensorTelemetryAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorTelemetry>
{
    public async Task ProcessMessageAsync(
        SensorTelemetry telemetry,
        AzureEventHubsMessageContext context,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        // Proces sensor telemetry...
    }
}
```

Testing these together requires us to register them into an application. The following example shows how instead of calling `.AddEventHubsMessagePump(...)`, you can call the testing variant `.AddTestEventHubsMessagePump(...)`.
The extension allows you to pass in a 'message producer'. This producer allows you to control which kind of messages the test message pump should simulate. This example shows how a single `SensorReading` message can be added to the message pump.

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
    public async Task RegisterSensorReadingAndSensorTelemetryMessageHandler_PublishSensorReading_ProcessSensorReadingCorrectly()
    {
        // Arrange
        var reading = new SensorReading(sensorId: "123", timestamp: DateTimeOffset.UtcNow);
        var handler = new SensorReadingAzureEventHubsMessageHandler();

        IHostBuilder builder =
            Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.AddXunitTestLogging(_outputWriter))
                .ConfigureServices(services =>
                {
                    services.AddTestEventHubsMessagePump(producer => producer.AddMessageBody(reading))
                            .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler)
                            .WithEventHubsMessageHandler<SensorTelemetryAzureEventHubsMessageHandler, SensorTelemetry>();
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

Note that in this example, we use `Assert.True(handler.IsProcessed)` to determine if the `SensorReading` message was correctly processed. In your application, you may want to inject your message handlers with test versions of dependencies and determine via those dependencies if the correct message handler was called.
You can use one of the `.WithEventHubsMessageHandler<,>(...)` extensions to pass in your instance of the message handler so you can use it later in the test assertion, like it is shown in the example.

> 💡 Note that the example uses `logging.AddXunitTestLogging`. This is available in the `Arcus.Testing.Logging` package. See [this page on logging](./logging.md) for more information.

## Message producer configuration
The test message pump can be configured extensively to meet your needs. You can even pass in your own implementation of a test message producer to have full control over how the Azure EventHubs messages should look like.
When a custom message body is passed, an `EventHubsReceivedMessage` will be created with the [correlation properties](https://messaging.arcus-azure.net/Features/message-handling/service-bus#message-correlation) already filled out. This allows for you to also test any the correlation specific functionality in your message handlers.

```csharp
services.AddTestEventHubsMessagePump(producer =>
{
    // Pass in a single custom message, which will become a `EventData`.
    producer.AddMessageBody(sensorReading);

    // Pass in multiple custom messages, which will become `EventData` instances.
    producer.AddMessageBodies(sensorReadings);

    // Pass in your own `EventData`.
    // 💡 Use Arcus' `EventDataBuilder` to create such messages.
    EventData message = 
      EventDataBuilder.CreateForBody(BinaryData.FromObjectAsJson(sensorReading))
                      .Build();
    producer.AddMessage(message);

    // Pass in your own `EventHubsReceivedMessage` instances.
    // 💡 Use Arcus' `EventDataBuilder` to create such messages.
    EventData message = 
      EventDataBuilder.CreateForBody(BinaryData.FromObjectAsJson(sensorReading))
                      .Build();
    producer.AddMessages(new[] { message });
});
```

If the creation of Azure EventHubs messages is rather complex, you require asynchronous serialization, or you want to re-use your message producing; you can implement your own message producer.
This requires you to implement the `IAzureEventHubsMessageProducer` interface.
```csharp
public class MyTestMessageProducer : IAzureEventHubsMessageProducer
{
    public async Task<EventData[]> ProduceMessagesAsync()
    {
        var reading = new SensorReading(sensorId: "123", timestamp: DateTimeOffset.UtcNow);

        EventData message = 
            EventDataBuilder.CreateForBody(BinaryData.FromObjectAsJson(reading))
                            .Build();

        return Task.FromResult(new[] { message });
    }
}
```

Such implementation can be pased along during the registration:
```csharp
var producer = new MyTestMessageProducer();
services.AddTestEventHubsMessagePump(producer);
```

## Message routing configuration
The test Azure EventHubs registration internally uses the Azure EventHubs message router. To configure this router, use one of the extension overloads. This gives you access to the message router options, like in a general Azure EventHubs message pump registration.
```csharp
services.AddTestEventHubsMessagePump(..., options =>
{
    options.Deserialization.AdditionalMembers = AdditionalMemberHandling.Error
});
```

For more information on the message router options, see the [Arcus messaging feature documentation](https://messaging.arcus-azure.net/Features/message-handling/event-hubs#pump-configuration).