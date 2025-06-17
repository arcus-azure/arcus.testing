﻿using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.ServiceBus.Administration;
using Xunit;
using Xunit.Abstractions;
using static Azure.Messaging.ServiceBus.Administration.CreateRuleOptions;

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

        private static RuleFilter AnyFilter => Bogus.PickRandom(new TrueRuleFilter(), new FalseRuleFilter(), new SqlRuleFilter("1=1"));

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

            string ourRuleName = await temp.WhenRuleAvailableAsync();
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, ourRuleName);

            await temp.DisposeAsync();
            await serviceBus.ShouldNotHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await serviceBus.ShouldNotHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, ourRuleName);
        }

        [Fact]
        public async Task CreateTempTopicSubscription_OnExistingSubscription_SucceedsByLeavingAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            string theirRuleName = await serviceBus.WhenTopicSubscriptionRuleAvailableAsync(topicName, subscriptionName);

            // Act
            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, theirRuleName);
            
            string ourRuleName = await temp.WhenRuleAvailableAsync();
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, ourRuleName);
            
            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await serviceBus.ShouldHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, theirRuleName);
            await serviceBus.ShouldNotHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, ourRuleName);
        }

        [Fact]
        public async Task CreateTempTopicSubscriptionWithRule_OnExistingSubscriptionWhenRuleDeletedOutsideScopeFixture_SucceedsByIgnoringAlreadyDeletedRule()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);

            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);
            string ruleName = await temp.WhenRuleAvailableAsync();

            await serviceBus.WhenTopicSubscriptionRuleDeletedAsync(topicName, subscriptionName, ruleName);

            // Act
            await temp.DisposeAsync();

            // Assert
            await serviceBus.ShouldHaveTopicSubscriptionAsync(topicName, subscriptionName);
            await serviceBus.ShouldNotHaveTopicSubscriptionRuleAsync(topicName, subscriptionName, ruleName);
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
        public async Task AdTopicSubscriptionRule_WithDefaultRuleName_FailsWithUnsupported()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = serviceBus.WhenTopicSubscriptionUnavailable(topicName);

            TemporaryTopicSubscription temp = await CreateTempTopicSubscriptionAsync(topicName, subscriptionName);

            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<ArgumentException>(
                () => temp.AddRuleIfNotExistsAsync(DefaultRuleName, AnyFilter));

            Assert.Contains("topic subscription", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("custom name", exception.Message, StringComparison.OrdinalIgnoreCase);
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
            string fullyQualifiedNamespace = Configuration.GetServiceBus().HostName;

            var temp = 
                Bogus.Random.Bool()
                    ? await TemporaryTopicSubscription.CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, subscriptionName, Logger)
                    : await TemporaryTopicSubscription.CreateIfNotExistsAsync(fullyQualifiedNamespace, "otherTopic", subscriptionName, Logger, configureOptions: options =>
                    {
                        options.OnSetup.CreateSubscriptionWith(sub => sub.TopicName = topicName);
                    });

            Assert.Equal(subscriptionName, temp.Name);
            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }

    internal static class TemporaryTopicSubscriptionExtensions
    {
        internal static async Task<string> WhenRuleAvailableAsync(this TemporaryTopicSubscription temp)
        {
            string ruleName = $"rule-{Guid.NewGuid()}";
            await temp.AddRuleIfNotExistsAsync(ruleName, new TrueRuleFilter());

            return ruleName;
        }
    }
}