using System;
using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
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
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(@namespace, "<queue-name>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempQueue_WithoutQueue_Fails(string queueName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync("<namespace>", queueName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync("<namespace>", queueName, NullLogger.Instance, configureOptions: opt => { }));

            var client = new Mock<ServiceBusAdministrationClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(client.Object, queueName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(client.Object, queueName, NullLogger.Instance, configureOptions: opt => { }));
        }

        [Fact]
        public async Task CreateTempQueue_WithoutClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient: null, "<queue-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryQueue.CreateIfNotExistsAsync(adminClient: null, "<queue-name>", NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}
