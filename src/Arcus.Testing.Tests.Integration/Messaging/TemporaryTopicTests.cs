using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.ServiceBus;
using Xunit;

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
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            ServiceBusMessage messageBefore = await serviceBus.WhenMessageSentAsync(topicName);

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName);

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldLeaveMessageAsync(topicName, subscriptionName, messageBefore);

            ServiceBusMessage messageAfter = serviceBus.WhenMessageUnsent();
            await temp.Sender.SendMessageAsync(messageAfter, TestContext.Current.CancellationToken);

            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageAfter);
        }

        [Fact]
        public async Task CreateTempTopicWithDeadLetterOnSetup_OnExistingTopicWithMessage_SucceedsByDeadLetteringMessageDuringCreationFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            ServiceBusMessage messageBefore = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeComplete1 = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeComplete2 = await serviceBus.WhenMessageSentAsync(topicName);

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName, options =>
            {
                options.OnSetup.DeadLetterMessages()
                               .CompleteMessages(msg => msg.MessageId == messageBeforeComplete1.MessageId)
                               .CompleteMessages(msg => msg.MessageId == messageBeforeComplete2.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBefore);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBeforeComplete1);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBeforeComplete2);

            Assert.Empty(await temp.MessagesOn(subscriptionName).Take(10).Where(msg => msg.MessageId == messageBeforeComplete1.MessageId || msg.MessageId == messageBeforeComplete2.MessageId));
            Assert.Single(await temp.MessagesOn(subscriptionName).FromDeadLetter().Where(msg => msg.MessageId == messageBefore.MessageId));

            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicAsync(topicName);
        }

        [Fact]
        public async Task CreateTempTopicWithCompleteOnSetup_OnExistingTopicWithMessage_SucceedsByCompletingMessageDuringCreationFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            ServiceBusMessage messageBefore = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeDeadLetter1 = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeDeadLetter2 = await serviceBus.WhenMessageSentAsync(topicName);

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName, options =>
            {
                options.OnSetup.CompleteMessages()
                               .DeadLetterMessages(msg => msg.MessageId == messageBeforeDeadLetter1.MessageId)
                               .DeadLetterMessages(msg => msg.MessageId == messageBeforeDeadLetter2.MessageId)
                               .CompleteMessages(msg => msg.MessageId == messageBeforeDeadLetter1.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBeforeDeadLetter1);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBeforeDeadLetter2);

            await temp.DisposeAsync();
            await serviceBus.ShouldHaveTopicAsync(topicName);
        }

        [Fact]
        public async Task CreateTempTopicWithDeadLetterOnTeardown_OnExistingTopicWithMessage_SucceedsByDeadLetteringMessageAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            ServiceBusMessage messageBefore = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeComplete1 = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeComplete2 = await serviceBus.WhenMessageSentAsync(topicName);

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName, options =>
            {
                options.OnTeardown.DeadLetterMessages()
                                  .CompleteMessages(msg => msg.MessageId == messageBeforeComplete1.MessageId)
                                  .CompleteMessages(msg => msg.MessageId == messageBeforeComplete2.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldLeaveMessageAsync(topicName, subscriptionName, messageBefore);

            ServiceBusMessage messageAfter = serviceBus.WhenMessageUnsent();
            await temp.Sender.SendMessageAsync(messageAfter, TestContext.Current.CancellationToken);

            await temp.DisposeAsync();
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageAfter);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBeforeComplete1);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBeforeComplete2);
        }

        [Fact]
        public async Task CreateTempTopicWithCompleteOnTeardown_OnExistingTopicWithMessage_SucceedsByCompletingMessageAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string topicName = await serviceBus.WhenTopicAvailableAsync();
            string subscriptionName = await serviceBus.WhenTopicSubscriptionAvailableAsync(topicName);
            ServiceBusMessage messageBefore1 = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeDeadLetter1 = await serviceBus.WhenMessageSentAsync(topicName);
            ServiceBusMessage messageBeforeDeadLetter2 = await serviceBus.WhenMessageSentAsync(topicName);

            // Act
            TemporaryTopic temp = await CreateTempTopicAsync(topicName, options =>
            {
                options.OnTeardown.CompleteMessages()
                                  .DeadLetterMessages(msg => msg.MessageId == messageBeforeDeadLetter1.MessageId)
                                  .DeadLetterMessages(msg => msg.MessageId == messageBeforeDeadLetter2.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveTopicAsync(topicName);
            await serviceBus.ShouldLeaveMessageAsync(topicName, subscriptionName, messageBefore1);

            ServiceBusMessage messageDeadLetterAfter = await serviceBus.WhenMessageSentAsync(topicName);
            temp.OnTeardown.DeadLetterMessages(msg => msg.MessageId == messageDeadLetterAfter.MessageId);

            ServiceBusMessage messageAfter = serviceBus.WhenMessageUnsent();
            await temp.Sender.SendMessageAsync(messageAfter, TestContext.Current.CancellationToken);

            await temp.DisposeAsync();
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageBefore1);
            await serviceBus.ShouldCompletedMessageAsync(topicName, subscriptionName, messageAfter);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBeforeDeadLetter1);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageBeforeDeadLetter2);
            await serviceBus.ShouldDeadLetteredMessageAsync(topicName, subscriptionName, messageDeadLetterAfter);
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

        private async Task<TemporaryTopic> CreateTempTopicAsync(string topicName, Action<TemporaryTopicOptions> configureOptions = null)
        {
            string fullyQualifiedNamespace = Configuration.GetServiceBus().HostName;
            var temp =
                configureOptions is null
                    ? await TemporaryTopic.CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, Logger)
                    : await TemporaryTopic.CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, Logger, configureOptions: options =>
                    {
                        options.OnSetup.CreateTopicWith(topic => topic.Name = topicName);
                        configureOptions(options);
                    });

            Assert.Equal(topicName, temp.Name);
            Assert.Equal(fullyQualifiedNamespace, temp.FullyQualifiedNamespace);

            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
