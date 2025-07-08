using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus
{
    public class TemporaryQueueTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempQueue_WithoutNamespace_Fails(string @namespace)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(@namespace, "<queue-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(@namespace, "<queue-name>", NullLogger.Instance, configureOptions: _ => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempQueue_WithoutQueue_Fails(string queueName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync("<namespace>", queueName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync("<namespace>", queueName, NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = new Mock<ServiceBusAdministrationClient>();
            var messagingClient = new Mock<ServiceBusClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient.Object, messagingClient.Object, queueName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient.Object, messagingClient.Object, queueName, NullLogger.Instance, configureOptions: _ => { }));
        }

        [Fact]
        public async Task CreateTempQueue_WithoutClient_Fails()
        {
            var messagingClient = new Mock<ServiceBusClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient: null, messagingClient.Object, "<queue-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient: null, messagingClient.Object, "<queue-name>", NullLogger.Instance, configureOptions: _ => { }));

            var adminClient = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient.Object, messagingClient: null, "<queue-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient.Object, messagingClient: null, "<queue-name>", NullLogger.Instance, configureOptions: _ => { }));
        }
    }
}
