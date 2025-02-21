using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus
{
    public class TemporaryTopicTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopic_WithoutNamespace_Fails(string @namespace)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(@namespace, "<topic-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(@namespace, "<topic-name>", NullLogger.Instance, configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopic_WithoutTopic_Fails(string topicName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync("<namespace>", topicName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync("<namespace>", topicName, NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = new Mock<ServiceBusAdministrationClient>();
            var messagingClient = new Mock<ServiceBusClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient.Object, messagingClient.Object, topicName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient.Object, messagingClient.Object, topicName, NullLogger.Instance, configureOptions: _ => { }));
        }

        [Fact]
        public async Task CreateTempTopic_WithoutClient_Fails()
        {
            var messagingClient = new Mock<ServiceBusClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient: null, messagingClient.Object, "<topic-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient: null, messagingClient.Object, "<topic-name>", NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient.Object, messagingClient: null, "<topic-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient.Object, messagingClient: null, "<topic-name>", NullLogger.Instance, configureOptions: _ => { }));
        }
    }
}
