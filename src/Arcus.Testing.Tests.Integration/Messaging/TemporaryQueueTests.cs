using System;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.ServiceBus;
using Xunit;

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

            ServiceBusMessage messageAfter = await serviceBus.WhenMessageSentAsync(queueName);

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
            await serviceBus.ShouldCompletedMessageAsync(queueName, messageCompleteBefore);

            Assert.True(await temp.Messages.FromDeadLetter().Where(msg => msg.MessageId == messageDeadLetterBefore.MessageId).AnyAsync(TestContext.Current.CancellationToken), $"temp queue should have found dead-lettered message '{messageDeadLetterBefore.MessageId}'");
            Assert.False(await temp.Messages.FromDeadLetter().Where(msg => msg.MessageId == messageCompleteBefore.MessageId).AnyAsync(TestContext.Current.CancellationToken), $"temp queue should not have found completed message '{messageCompleteBefore.MessageId}'");

            ServiceBusMessage messageAfter = await serviceBus.WhenMessageSentAsync(queueName);

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
            TemporaryQueue temp = await CreateTempQueueAsync(queueName, options =>
            {
                options.OnSetup.CompleteMessages()
                               .DeadLetterMessages(msg => msg.MessageId == messageDeadLetterBefore.MessageId)
                               .CompleteMessages(msg => msg.MessageId == messageDeadLetterBefore.MessageId);
            });

            // Assert
            await serviceBus.ShouldHaveQueueAsync(queueName);
            await serviceBus.ShouldCompletedMessageAsync(queueName, messageCompleteBefore);
            await serviceBus.ShouldDeadLetteredMessageAsync(queueName, messageDeadLetterBefore);

            Assert.Empty(await temp.Messages.Where(msg => msg.MessageId == messageCompleteBefore.MessageId));
            Assert.Single(await temp.Messages.FromDeadLetter().Take(10).Where(msg => msg.MessageId == messageDeadLetterBefore.MessageId));
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
            Assert.Single(await temp.Messages.Where(msg => msg.MessageId == messageCompleteBefore1.MessageId));

            ServiceBusMessage messageCompleteAfter = serviceBus.WhenMessageUnsent(),
                              messageDeadLetterAfter = serviceBus.WhenMessageUnsent();
            await serviceBus.WhenMessageSentAsync(queueName, messageCompleteAfter);
            await serviceBus.WhenMessageSentAsync(queueName, messageDeadLetterAfter);
            temp.OnTeardown.DeadLetterMessages(msg => msg.MessageId == messageDeadLetterAfter.MessageId);

            await temp.DisposeAsync();

            await serviceBus.ShouldCompletedMessageAsync(queueName, messageCompleteBefore1);
            await serviceBus.ShouldCompletedMessageAsync(queueName, messageCompleteBefore2);
            await serviceBus.ShouldCompletedMessageAsync(queueName, messageCompleteAfter);
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

                            configureOptions(options);
                        });

            Assert.Equal(queueName, temp.Name);
            Assert.Equal(fullyQualifiedNamespace, temp.FullyQualifiedNamespace);

            return temp;
        }

        private ServiceBusTestContext GivenServiceBus()
        {
            return ServiceBusTestContext.Given(Configuration, Logger);
        }
    }
}
