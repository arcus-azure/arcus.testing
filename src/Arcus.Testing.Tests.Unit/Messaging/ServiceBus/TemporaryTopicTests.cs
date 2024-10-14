using System;
using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
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
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(@namespace, "<topic-name>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTopic_WithoutTopic_Fails(string topicName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync("<namespace>", topicName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync("<namespace>", topicName, NullLogger.Instance, configureOptions: opt => { }));

            var client = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(client.Object, topicName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(client.Object, topicName, NullLogger.Instance, configureOptions: opt => { }));
        }

        [Fact]
        public async Task CreateTempTopic_WithoutClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient: null, "<topic-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTopic.CreateIfNotExistsAsync(adminClient: null, "<topic-name>", NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}
