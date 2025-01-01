using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.ServiceBus.Administration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Messaging
{
    public class TemporaryTopicSubscriptionRuleTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryTopicSubscriptionRuleTests" /> class.
        /// </summary>
        public TemporaryTopicSubscriptionRuleTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempSubscriptionRule_WithNonExistingRule_SucceedsByExistingDuringLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);

            // Act
            TemporaryTopicSubscriptionRule temp = await CreateTempRuleAsync(topicName, subscriptionName);

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, temp.Name);

            await temp.DisposeAsync();
            await serviceBus.ShouldNotHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, temp.Name);
        }

        [Fact]
        public async Task CreateTempSubscriptionRule_WithExistingRule_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            RuleProperties rule = await serviceBus.WhenTopicSubscriptionRuleAvailableAsync(topicName, subscriptionName);

            // Act
            TemporaryTopicSubscriptionRule temp = await CreateTempRuleAsync(topicName, subscriptionName, rule.Name);

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, rule.Name, actual => Assert.NotEqual(rule, actual));

            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, rule.Name, actual => Assert.Equal(rule, actual));
        }

        [Fact]
        public async Task CreateTempSubscriptionRule_WithNonExistingTopicSubscription_FailsWithSetupError()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();

            // Act / Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateTempRuleAsync(topicName, "not-existing-subscription-name", "ignored-rule-name"));

            Assert.Contains("Azure Service bus topic subscription", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateTempSubscriptionRule_WithNonExistingTopic_FailsWithSetupError()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            // Act / Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateTempRuleAsync("not-existing-topic", "ignored-subscription-name", "ignored-rule-name"));

            Assert.Contains("Azure Service bus topic", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }

        private async Task<TemporaryTopicSubscriptionRule> CreateTempRuleAsync(string topicName, string subscriptionName, RuleProperties rule = null)
        {
            string ruleName = rule?.Name ?? $"rule-{Bogus.Random.Guid()}";
            return await CreateTempRuleAsync(topicName, subscriptionName, ruleName);
        }

        private async Task<TemporaryTopicSubscriptionRule> CreateTempRuleAsync(string topicName, string subscriptionName, string ruleName)
        {
            string fullyQualifiedNamespace = Configuration.GetServiceBus().HostName;


            return await TemporaryTopicSubscriptionRule.CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, subscriptionName, ruleName, Logger, configureOptions: 
                options =>
                {
                    options.Filter = Bogus.PickRandom<RuleFilter>(
                        new TrueRuleFilter(),
                        new FalseRuleFilter(),
                        new SqlRuleFilter("1=1"),
                        new CorrelationRuleFilter(Bogus.Random.Guid().ToString()));

                    options.Action = new SqlRuleAction($"SET sys.CorrelationId = '{Bogus.Random.Guid()}';");
                });
        }
    }
}
