using System;
using System.Threading.Tasks;
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
        public async Task CreateTempTopicSubscription_WithoutNamespace_Fails(string fullyQualifiedNamespace)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(fullyQualifiedNamespace, "<topic-name>", "<subscription-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(fullyQualifiedNamespace, "<topic-name>", "<subscription-name>", NullLogger.Instance, configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopicSubscription_WithoutTopicName_Fails(string topicName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", topicName, "<subscription-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", topicName, "<subscription-name>", NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = Mock.Of<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient, topicName, "<subscription-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient, topicName, "<subscription-name>", NullLogger.Instance, configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopicSubscription_WithoutSubscriptionName_Fails(string subscriptionName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", "<topic-name>", subscriptionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync("<namespace>", "<topic-name>", subscriptionName, NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = Mock.Of<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient, "<topic-name>", subscriptionName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient, "<topic-name>", subscriptionName, NullLogger.Instance, configureOptions: _ => { }));
        }

        [Fact]
        public async Task CreateTempTopicSubscription_WithoutAdminClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient: null, "<topic-name>", "<subscription-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopicSubscription.CreateIfNotExistsAsync(adminClient: null, "<topic-name>", "<subscription-name>", NullLogger.Instance, configureOptions: _ => { }));
        }
    }
}
