using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.EventHubs.Consumer;
using Azure.ResourceManager.EventHubs;
using Bogus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging
{
    public class TemporaryEventHubTests
    {
        private static readonly Faker Bogus = new();

        private static ResourceIdentifier NamespaceResourceId => ResourceIdentifier.Root;
        private static EventHubsNamespaceResource NamespaceResource => Mock.Of<EventHubsNamespaceResource>();
        private static string ConsumerGroup => Bogus.Lorem.Word();
        private static EventHubConsumerClient ConsumerClient => Mock.Of<EventHubConsumerClient>();
        private static string EventHubName => Bogus.Lorem.Word();
        private static ILogger Logger => NullLogger.Instance;
        private static Action<TemporaryEventHubOptions> ConfigureOptions => _ => { };

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempHub_WithoutEventHubName_Fails(string eventHubName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResourceId, ConsumerGroup, eventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResourceId, ConsumerGroup, eventHubName, Logger, ConfigureOptions));

            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResource, ConsumerClient, eventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResource, ConsumerClient, eventHubName, Logger, ConfigureOptions));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempHub_WithoutConsumerGroup_Fails(string consumerGroup)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResourceId, consumerGroup, EventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResourceId, consumerGroup, EventHubName, Logger, ConfigureOptions));
        }

        [Fact]
        public async Task CreateTempHub_WithoutNamespaceResourceId_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(eventHubsNamespaceResourceId: null, ConsumerGroup, EventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(eventHubsNamespaceResourceId: null, ConsumerGroup, EventHubName, Logger, ConfigureOptions));

        }

        [Fact]
        public async Task CreateTempHub_WithoutNamespaceResource_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(eventHubsNamespace: null, ConsumerClient, EventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(eventHubsNamespace: null, ConsumerClient, EventHubName, Logger, ConfigureOptions));
        }

        [Fact]
        public async Task CreateTempHub_WithoutConsumerClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResource, consumerClient: null, EventHubName, Logger));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryEventHub.CreateIfNotExistsAsync(NamespaceResource, consumerClient: null, EventHubName, Logger, ConfigureOptions));
        }
    }
}
