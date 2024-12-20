using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Messaging
{
    public class TemporaryTopicTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTopicTests" /> class.
        /// </summary>
        public TemporaryTopicTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempTopic_OnNonExistingTopic_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = serviceBus.WhenTopicUnavailable();
            
            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName);

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await temp.DisposeAsync();
            await serviceBus.ShouldNotHaveTopicAsync(topicName);
        }

        [Fact]
        public async Task CreateTempTopic_OnExistingTopic_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName);

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicAsync(topicName);
        }

        [Fact]
        public async Task CreateTempTopic_OnNonExistingTopicWhenDeletedOutsideScopeFixture_SucceedsByAlreadyDeletedTopic()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = serviceBus.WhenTopicUnavailable();
            TemporaryTopic temp = await CreateTempTopicAsync(topicName);

            await serviceBus.WhenTopicDeletedAsync(topicName);

            // Act
            await temp.DisposeAsync();

            // Assert
            await serviceBus.ShouldNotHaveTopicAsync(topicName);
        }

        [Fact]
        public async Task CreateTempTopic_OnExistingTopicWithSubscriptions_SucceedsByOnlyDeletingTrackedSubscriptions()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string createdBeforeOutOfScope = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);

            TemporaryTopic temp = await CreateTempTopicAsync(topicName);

            string createdAfterOutOfScope = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            string createdByUs = serviceBus.WhenTopicSubscriptionUnavailable(topicName);
            await temp.AddSubscriptionAsync(createdByUs);

            // Act
            await temp.DisposeAsync();

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, createdBeforeOutOfScope);
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, createdAfterOutOfScope);
            await serviceBus.ShouldNotHaveTopicSubscriptionAsync(topicName, createdByUs);
        }

        private async Task<TemporaryTopic> CreateTempTopicAsync(string topicName)
        {
            var temp = await TemporaryTopic.CreateIfNotExistsAsync(Configuration.GetServiceBus().HostName, topicName, Logger);
            Assert.Equal(topicName, temp.Name);

            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
