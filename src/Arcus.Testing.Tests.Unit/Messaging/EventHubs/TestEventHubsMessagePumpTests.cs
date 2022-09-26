using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Unit.Messaging.EventHubs.Fixture;
using Azure.Messaging.EventHubs;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Messaging.EventHubs
{
    public class TestEventHubsMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestEventHubsMessagePumpTests" /> class.
        /// </summary>
        public TestEventHubsMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task TestMessagePump_WithSingleMessageBody_ProcessesCorrectly()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler1 = new SensorReadingAzureEventHubsMessageHandler(reading);
            var handler2 = new SensorTelemetryAzureEventHubsMessageHandler();

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessageBody(reading))
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler1)
                                    .WithEventHubsMessageHandler<SensorTelemetryAzureEventHubsMessageHandler, SensorTelemetry>(provider => handler2), 
                () =>
                {
                    Assert.True(handler1.IsProcessed);
                    Assert.False(handler2.IsProcessed);
                });
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownSingleMessageBody_DoesNotProcesses()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler = new SensorReadingAzureEventHubsMessageHandler(reading);
            var telemetry = SensorTelemetry.Generate();

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessageBody(telemetry))
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithMultipleMessageBodies_ProcessesCorrectly()
        {
            // Arrange
            IEnumerable<SensorReading> readings = BogusGenerator.Make(10, SensorReading.Generate);
            var handler = new SensorReadingAzureEventHubsMessageHandler(readings.ToArray());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessageBodies(readings))
                                    .WithEventHubsMessageHandler<SensorTelemetryAzureEventHubsMessageHandler, SensorTelemetry>()
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownMultipleMessageBodies_DoesNotProcesses()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler = new SensorReadingAzureEventHubsMessageHandler(reading);
            IEnumerable<SensorTelemetry> telemetries = BogusGenerator.Make(10, SensorTelemetry.Generate);

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessageBodies(telemetries))
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithSingleMessage_ProcessesCorrectly()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler = new SensorReadingAzureEventHubsMessageHandler(reading);
            EventData message = EventDataBuilder.CreateForBody(reading).Build();

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessage(message))
                                    .WithEventHubsMessageHandler<SensorTelemetryAzureEventHubsMessageHandler, SensorTelemetry>()
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownSingleMessage_DoesNotProcesses()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler = new SensorReadingAzureEventHubsMessageHandler(reading);
            var telemetry = SensorTelemetry.Generate();
            EventData message = EventDataBuilder.CreateForBody(telemetry).Build();

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessage(message))
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.False(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithMultipleMessages_ProcessesCorrectly()
        {
            // Arrange
            IEnumerable<SensorReading> readings = BogusGenerator.Make(10, SensorReading.Generate);
            var handler = new SensorReadingAzureEventHubsMessageHandler(readings.ToArray());
            IEnumerable<EventData> messages = readings.Select(reading => EventDataBuilder.CreateForBody(reading).Build());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessages(messages))
                                    .WithEventHubsMessageHandler<SensorTelemetryAzureEventHubsMessageHandler, SensorTelemetry>()
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
                () => Assert.True(handler.IsProcessed));
        }

        [Fact]
        public async Task TestMessagePump_WithUnknownMultipleMessages_DoesNotProcesses()
        {
            // Arrange
            var reading = SensorReading.Generate();
            var handler = new SensorReadingAzureEventHubsMessageHandler(reading);
            IEnumerable<SensorTelemetry> telemetries = BogusGenerator.Make(10, SensorTelemetry.Generate);
            IEnumerable<EventData> messages = telemetries.Select(telemetry => EventDataBuilder.CreateForBody(telemetry).Build());

            // Act / Assert
            await StartNewHostAsync(
                services => services.AddTestEventHubsMessagePump(produce => produce.AddMessages(messages))
                                    .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>(provider => handler), 
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
