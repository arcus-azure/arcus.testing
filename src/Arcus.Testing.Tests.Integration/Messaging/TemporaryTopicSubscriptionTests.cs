using System;
using System.Threading.Tasks;
using Arcus.Testing.Messaging.ServiceBus;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Messaging
{
    public class TemporaryTopicSubscriptionTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTopicSubscriptionTests" /> class.
        /// </summary>
        public TemporaryTopicSubscriptionTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempTopicSubscription_OnNonExistingSubscription_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = serviceBus.WhenTopicSubscriptionUnavailable(topicName);

            // Act
            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await temp.DisposeAsync();
            await serviceBus.ShouldNotHaveTopicSubscriptionAsync(topicName, subscriptionName);
        }

        [Fact]
        public async Task CreateTempTopicSubscription_OnExistingSubscription_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            
            // Act
            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
        }

        [Fact]
        public async Task CreateTempTopicSubscription_OnNonExistingSubscriptionWhenDeletedOutsideScopeFixture_SucceedsByIgnoringAlreadyDeletedSubscription()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = serviceBus.WhenTopicSubscriptionUnavailable(topicName);
            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);

            await serviceBus.WhenTopicSubscriptionDeletedAsync(topicName, subscriptionName);

            // Act
            await temp.DisposeAsync();

            // Assert
            await serviceBus.ShouldNotHaveTopicSubscriptionAsync(topicName, subscriptionName);
        }

        [Fact]
        public async Task CreateTempTopicSubscription_OnNonExistingTopic_FailsWithSetupError()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            // Act / Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateTempTopicSubscriptionAsync("non-existing-topic", "ignored-subscription-name"));

            Assert.Contains("Azure Service bus topic", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<TemporaryTopicSubscription> CreateTempTopicSubscriptionAsync(string topicName, string subscriptionName)
        {
            var temp = await TemporaryTopicSubscription.CreateIfNotExistsAsync(Configuration.GetServiceBus().HostName, topicName, subscriptionName, Logger);

            Assert.Equal(subscriptionName, temp.Name);
            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
