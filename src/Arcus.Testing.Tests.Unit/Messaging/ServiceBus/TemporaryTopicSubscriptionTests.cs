using System;
using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus
{
    public class TemporaryTopicSubscriptionTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopicSubscription_WithoutNamespace_Fails(string @namespace)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(@namespace, "<topic-name>", "<subscription-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(@namespace, "<topic-name>", "<subscription-name>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopicSubscription_WithoutTopicName_Fails(string topicName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", topicName, "<subscription>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", topicName, "<subscription>", NullLogger.Instance, configureOptions: opt => { }));

            var client = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(client.Object, topicName, "<subscription>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(client.Object, topicName, "<subscription>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopicSubscription_WithoutSubscriptionName_Fails(string subscriptionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", "<topic>", subscriptionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", "<topic>", subscriptionName, NullLogger.Instance, configureOptions: opt => { }));

            var client = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(client.Object, "<topic>", subscriptionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(client.Object, "<topic>", subscriptionName, NullLogger.Instance, configureOptions: opt => { }));
        }

        [Fact]
        public async Task CreateTempTopicSubscription_WithoutClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient: null, "<topic>", "<subscription>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient: null, "<topic>", "<subscription>", NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}
