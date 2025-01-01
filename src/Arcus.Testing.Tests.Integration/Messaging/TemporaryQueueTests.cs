using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.ServiceBus;
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
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = await serviceBus.WhenQueueAvailableAsync();
            ServiceBusMessage messageBefore = await serviceBus.WhenMessageSentAsync(queueName);

            // Act
            TemporaryQueue temp = await CreateTempQueueAsync(queueName);

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldLeaveMessageAsync(queueName, messageBefore);

            ServiceBusMessage messageAfter = serviceBus.WhenMessageUnsent();
            await temp.SendMessageAsync(messageAfter);

            await temp.DisposeAsync();
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageAfter);
        }

        [Fact]
        public async Task CreateTempQueueWithDeadLetterOnSetup_OnExistingQueueWithMessage_SucceedsByDeadLetteringMessageDuringCreationFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = await serviceBus.WhenQueueAvailableAsync();
            ServiceBusMessage messageDeadLetterBefore = await serviceBus.WhenMessageSentAsync(queueName);
            ServiceBusMessage messageCompleteBefore = await serviceBus.WhenMessageSentAsync(queueName);

            // Act
            TemporaryQueue temp = await CreateTempQueueAsync(queueName, options =>
            {
                options.OnSetup.DeadLetterMessages()
                               .CompleteMessages(msg => msg.MessageId == messageCompleteBefore.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageDeadLetterBefore);
            await serviceBus.ShouldCompleteMessageAsync(queueName, messageCompleteBefore);

            ServiceBusMessage messageAfter = serviceBus.WhenMessageUnsent();
            await temp.SendMessageAsync(messageAfter);

            await temp.DisposeAsync();
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageAfter);
        }

        [Fact]
        public async Task CreateTempQueueWithCompleteOnSetup_OnExistingQueueWithMessage_SucceedsByCompletingMessageDuringCreationFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = await serviceBus.WhenQueueAvailableAsync();
            ServiceBusMessage messageCompleteBefore = await serviceBus.WhenMessageSentAsync(queueName);
            ServiceBusMessage messageDeadLetterBefore = await serviceBus.WhenMessageSentAsync(queueName);

            // Act
            _ = await CreateTempQueueAsync(queueName, options =>
            {
                options.OnSetup.CompleteMessages()
                               .DeadLetterMessages(msg => msg.MessageId == messageDeadLetterBefore.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldCompleteMessageAsync(queueName, messageCompleteBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageDeadLetterBefore);
        }

        [Fact]
        public async Task CreateTempQueueWithCompleteOnTeardown_OnExistingQueueWithMessage_SucceedsByCompletingMessagingAfterLifetimeFixture()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string queueName = await serviceBus.WhenQueueAvailableAsync();
            ServiceBusMessage messageCompleteBefore1 = await serviceBus.WhenMessageSentAsync(queueName);
            ServiceBusMessage messageCompleteBefore2 = await serviceBus.WhenMessageSentAsync(queueName);
            ServiceBusMessage messageDeadLetterBefore = await serviceBus.WhenMessageSentAsync(queueName);

            // Act
            TemporaryQueue temp = await CreateTempQueueAsync(queueName, options =>
            {
                options.OnTeardown.CompleteMessages()
                                  .DeadLetterMessages(msg => msg.MessageId == messageDeadLetterBefore.MessageId)
                                  .CompleteMessages(msg => msg.MessageId == messageCompleteBefore2.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldLeaveMessageAsync(queueName, messageCompleteBefore1);

            ServiceBusMessage messageCompleteAfter = serviceBus.WhenMessageUnsent(),
                              messageDeadLetterAfter = serviceBus.WhenMessageUnsent();
            await temp.Sender.SendMessagesAsync([messageCompleteAfter, messageDeadLetterAfter]);
            temp.OnTeardown.DeadLetterMessages(msg => msg.MessageId == messageDeadLetterAfter.MessageId);

            await temp.DisposeAsync();

            await serviceBus.ShouldCompleteMessageAsync(queueName, messageCompleteBefore1);
            await serviceBus.ShouldCompleteMessageAsync(queueName, messageCompleteBefore2);
            await serviceBus.ShouldCompleteMessageAsync(queueName, messageCompleteAfter);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageDeadLetterAfter);
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

        private async Task<TemporaryQueue> CreateTempQueueAsync(string queueName, Action<TemporaryQueueOptions> configureOptions = null)
        {
            string fullyQualifiedNamespace = Configuration.GetServiceBus().HostName;

            var temp = 
                configureOptions is null
                    ? await TemporaryQueue.CreateIfNotExistsAsync(fullyQualifiedNamespace, queueName, Logger)
                    : await TemporaryQueue.CreateIfNotExistsAsync(fullyQualifiedNamespace, Bogus.Random.Guid().ToString(), Logger, configureOptions:
                        options =>
                        {
                            options.OnSetup.CreateQueueWith(queue => queue.Name = Bogus.Random.Guid().ToString())
                                           .CreateQueueWith(queue => queue.Name = queueName);

                            options.OnSetup.WaitMaxForMessages(TimeSpan.FromSeconds(5));
                            options.OnTeardown.WaitMaxForMessages(TimeSpan.FromSeconds(5));

                            configureOptions(options);
                        });

            Assert.Equal(queueName, temp.Name);
            Assert.Equal(queueName, temp.Sender.EntityPath);
            Assert.Equal(queueName, temp.Receiver.EntityPath);

            Assert.Equal(fullyQualifiedNamespace, temp.Sender.FullyQualifiedNamespace);
            Assert.Equal(fullyQualifiedNamespace, temp.Receiver.FullyQualifiedNamespace);

            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
