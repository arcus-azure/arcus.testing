using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Messaging
{
    public class TemporaryQueueTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryQueueTests" /> class.
        /// </summary>
        public TemporaryQueueTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempQueue_OnNonExistingQueue_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = serviceBus.WhenQueueUnavailable();

            // Act
            TemporaryQueue temp = await CreateTempQueueAsync(queueName);

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await temp.DisposeAsync();
            await serviceBus.ShouldNotHaveQueueAsync(queueName);
        }

        [Fact]
        public async Task CreateTempQueue_OnExistingQueue_SucceedsByLeavingAfterLifetimeFixture()
        {
            // arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = await serviceBus.WhenQueueAvailableAsync();

            // Act
            TemporaryQueue temp = await CreateTempQueueAsync(queueName);

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await temp.DisposeAsync();
            await serviceBus.ShouldHaveQueueAsync(queueName);
        }

        [Fact]
        public async Task CreateTempQueue_OnNonExistingQueueWhenQueueDeletedOutsideScopeFixture_SucceedsByIgnoringAlreadyDeletedQueue()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = serviceBus.WhenQueueUnavailable();
            TemporaryQueue temp = await CreateTempQueueAsync(queueName);
            await serviceBus.WhenQueueDeletedAsync(queueName);

            // Act
            await temp.DisposeAsync();

            // Assert
            await serviceBus.ShouldNotHaveQueueAsync(queueName);
        }

        private async Task<TemporaryQueue> CreateTempQueueAsync(string queueName)
        {
            var temp = await TemporaryQueue.CreateIfNotExistsAsync(Configuration.GetServiceBus().HostName, queueName, Logger);

            Assert.Equal(queueName, temp.Name);
            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
