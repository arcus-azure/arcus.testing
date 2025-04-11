﻿﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    internal enum OnSetupMessagesTopic { LeaveExistingMessages = 0, DeadLetterMessages, CompleteMessages }

    internal enum OnTeardownMessagesTopic { DeadLetterMessages = 0, CompleteMessages }

    /// <summary>
    /// Represents the available options when setting up the <see cref="TemporaryTopic"/>.
    /// </summary>
    public class OnSetupTemporaryTopicOptions
    {
        private readonly Collection<Action<CreateTopicOptions>> _configuredOptions = new();
        private readonly Collection<Func<ServiceBusReceivedMessage, bool>> _shouldCompleteMessages = new(), _shouldDeadLetterMessages = new();

        internal OnSetupMessagesTopic Messages { get; private set; }
        internal TimeSpan MaxWaitTime { get; private set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Configures the <see cref="Azure.Messaging.ServiceBus.Administration.CreateTopicOptions"/> used when the test fixture creates the topic.
        /// </summary>
        /// <remarks>
        ///     Multiple calls gets aggregated together.
        /// </remarks>
        /// <param name="configureOptions">The custom function to alter the way the topic gets created.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureOptions"/> is <c>null</c>.</exception>
        public OnSetupTemporaryTopicOptions CreateTopicWith(Action<CreateTopicOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            _configuredOptions.Add(configureOptions);

            return this;
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryTopic"/> to leave any existing messages be on the topic.
        /// </summary>
        public OnSetupTemporaryTopicOptions LeaveExistingMessages()
        {
            Messages = OnSetupMessagesTopic.LeaveExistingMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to dead-letter any pre-existing messages on the topic upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        public OnSetupTemporaryTopicOptions DeadLetterMessages()
        {
            return DeadLetterMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to dead-letter any pre-existing messages on the topic upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnSetupTemporaryTopicOptions DeadLetterMessages(TimeSpan maxWaitTime)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxWaitTime, TimeSpan.Zero, nameof(maxWaitTime));
            MaxWaitTime = maxWaitTime;
            Messages = OnSetupMessagesTopic.DeadLetterMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to dead-letter any pre-existing messages
        /// that matches the given <paramref name="messageFilter"/> on the topic upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="DeadLetterMessages(TimeSpan)"/>.
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be dead-lettered or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnSetupTemporaryTopicOptions DeadLetterMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldDeadLetterMessages.Add(messageFilter);

            return this;
        }

         /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any pre-existing messages on all available topic subscriptions
        /// upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        public OnSetupTemporaryTopicOptions CompleteMessages()
        {
            return CompleteMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any pre-existing messages on all available topic subscriptions
        /// upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnSetupTemporaryTopicOptions CompleteMessages(TimeSpan maxWaitTime)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxWaitTime, TimeSpan.Zero, nameof(maxWaitTime));
            MaxWaitTime = maxWaitTime;
            Messages = OnSetupMessagesTopic.CompleteMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any pre-existing messages
        /// that matches the given <paramref name="messageFilter"/> on the topic upon the creation of the test fixture.
        /// </summary>
        /// <param name="messageFilter">The custom filter to determine whether a message should be completed or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnSetupTemporaryTopicOptions CompleteMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldCompleteMessages.Add(messageFilter);

            return this;
        }

        internal CreateTopicOptions CreateTopicOptions(string name)
        {
            var options = new CreateTopicOptions(name);
            foreach (var action in _configuredOptions)
            {
                action(options);
            }

            return options;
        }

        internal MessageSettle DetermineMessageSettle(ServiceBusReceivedMessage message)
        {
            if (_shouldDeadLetterMessages.Any(func => func(message)))
            {
                return MessageSettle.DeadLetter;
            }

            if (_shouldCompleteMessages.Any(func => func(message)))
            {
                return MessageSettle.Complete;
            }

            if (Messages is OnSetupMessagesTopic.CompleteMessages)
            {
                return MessageSettle.Complete;
            }

            return MessageSettle.DeadLetter;
        }
    }

    /// <summary>
    /// Represents the available options when tearing down the <see cref="TemporaryTopic"/>.
    /// </summary>
    public class OnTeardownTemporaryTopicOptions
    {
        private readonly Collection<Func<ServiceBusReceivedMessage, bool>> _shouldCompleteMessages = new(), _shouldDeadLetterMessages = new();

        private OnTeardownMessagesTopic Messages { get; set; }
        private TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// (default) Configures the <see cref="TemporaryTopic"/> to dead-letter any remaining messages on the topic.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        public OnTeardownTemporaryTopicOptions DeadLetterMessages()
        {
            return DeadLetterMessages(MaxWaitTime);
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryTopic"/> to dead-letter any remaining messages on the topic.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnTeardownTemporaryTopicOptions DeadLetterMessages(TimeSpan maxWaitTime)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxWaitTime, TimeSpan.Zero, nameof(maxWaitTime));
            MaxWaitTime = maxWaitTime;
            Messages = OnTeardownMessagesTopic.DeadLetterMessages;

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the topic
        /// that matches the given <paramref name="messageFilter"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="DeadLetterMessages(TimeSpan)"/>.
        ///   </para>
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be dead-lettered or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnTeardownTemporaryTopicOptions DeadLetterMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldDeadLetterMessages.Add(messageFilter);

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any remaining messages on the topic.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        public OnTeardownTemporaryTopicOptions CompleteMessages()
        {
            return CompleteMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any remaining messages on the topic.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnTeardownTemporaryTopicOptions CompleteMessages(TimeSpan maxWaitTime)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxWaitTime, TimeSpan.Zero, nameof(maxWaitTime));
            MaxWaitTime = maxWaitTime;
            Messages = OnTeardownMessagesTopic.CompleteMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTopic"/> to complete any remaining messages on the topic.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering topic after the test run.
        ///   </para>
        ///   <para>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="CompleteMessages(TimeSpan)"/>.
        ///   </para>
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be completed or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnTeardownTemporaryTopicOptions CompleteMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldCompleteMessages.Add(messageFilter);

            return this;
        }

        internal MessageSettle DetermineMessageSettle(ServiceBusReceivedMessage message, ServiceBusReceiver receiver, ILogger logger)
        {
            bool shouldDeadLetter = _shouldDeadLetterMessages.Any(func => func(message));
            bool shouldComplete = _shouldCompleteMessages.Any(func => func(message));

            if (shouldDeadLetter && shouldComplete)
            {
                logger.LogWarning("[Test:Teardown] Service bus message '{MessageId}' matches both for dead-letter as completion in custom message filters, uses dead-letter, from topic subscription '{TopicSubscriptionName}' in namespace '{Namespace}'", message.MessageId, receiver.EntityPath, receiver.FullyQualifiedNamespace);
                return MessageSettle.DeadLetter;
            }

            if (shouldDeadLetter)
            {
                return MessageSettle.DeadLetter;
            }

            if (shouldComplete)
            {
                return MessageSettle.Complete;
            }

            if (Messages is OnTeardownMessagesTopic.CompleteMessages)
            {
                return MessageSettle.Complete;
            }

            return MessageSettle.DeadLetter;
        }
    }

    /// <summary>
    /// Represents the available options on the <see cref="TemporaryTopic"/>.
    /// </summary>
    public class TemporaryTopicOptions
    {
        /// <summary>
        /// Gets the options related to setting up the <see cref="TemporaryTopic"/>.
        /// </summary>
        public OnSetupTemporaryTopicOptions OnSetup { get; } = new OnSetupTemporaryTopicOptions().LeaveExistingMessages();

        /// <summary>
        /// Gets the options related to tearing down the <see cref="TemporaryTopic"/>.
        /// </summary>
        public OnTeardownTemporaryTopicOptions OnTeardown { get; } = new OnTeardownTemporaryTopicOptions().DeadLetterMessages();
    }

    /// <summary>
    /// Represents a temporary Azure Service Bus topic that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopic : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly ServiceBusClient _messagingClient;
        private readonly Collection<TemporaryTopicSubscription> _subscriptions = new();

        private readonly bool _topicCreatedByUs, _messagingClientCreatedByUs;
        private readonly TemporaryTopicOptions _options;
        private readonly ILogger _logger;

        private TemporaryTopic(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            bool messagingClientCreatedByUs,
            string topicName,
            bool topicCreatedByUs,
            TemporaryTopicOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentNullException.ThrowIfNull(messagingClient);

            _adminClient = adminClient;
            _messagingClient = messagingClient;
            _messagingClientCreatedByUs = messagingClientCreatedByUs;

            _topicCreatedByUs = topicCreatedByUs;
            
            _options = options ?? new TemporaryTopicOptions();
            _logger = logger;

            Name = topicName;
            FullyQualifiedNamespace = messagingClient.FullyQualifiedNamespace;
            Sender = messagingClient.CreateSender(topicName);
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the fully-qualified name of the Azure Service bus namespace for which this test fixture managed a topic.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Gets the client to send messages to this Azure Service bus test-managed topic.
        /// </summary>
        public ServiceBusSender Sender { get; }

        /// <summary>
        /// creates a filter topic subscription client to search for messages on the Azure Service bus test-managed topic (a.k.a. 'spy test fixture').
        /// </summary>
        /// <param name="subscriptionName">The subscription specific to the test-managed Azure Service bus topic to create a filter for.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> is blank.</exception>
        public ServiceBusMessageFilter MessagesOn(string subscriptionName) => new(Name, subscriptionName, _messagingClient);

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="topicName"/> is blank.
        /// </exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string topicName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic should be created.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="topicName"/> is blank.
        /// </exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string topicName,
            ILogger logger,
            Action<TemporaryTopicOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank fully-qualified Azure Service bus namespace to create a temporary topic", nameof(fullyQualifiedNamespace));
            }

            var credential = new DefaultAzureCredential();
            var adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);
            var messagingClient = new ServiceBusClient(fullyQualifiedNamespace, credential);

            return await CreateIfNotExistsAsync(adminClient, messagingClient, messagingClientCreatedByUs: true, topicName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="messagingClient">
        ///     The messaging client to both send and receive messages on the Azure Service bus, as well as handling setup and teardown actions.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string topicName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, messagingClient, topicName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="messagingClient">
        ///     The messaging client to both send and receive messages on the Azure Service bus, as well as handling setup and teardown actions.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string topicName,
            ILogger logger,
            Action<TemporaryTopicOptions> configureOptions)
        {
            return await CreateIfNotExistsAsync(adminClient, messagingClient, messagingClientCreatedByUs: false, topicName, logger, configureOptions);
        }

        private static async Task<TemporaryTopic> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            bool messagingClientCreatedByUs,
            string topicName,
            ILogger logger,
            Action<TemporaryTopicOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentNullException.ThrowIfNull(messagingClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service Bus topic name to create a temporary topic", nameof(topicName));
            }

            var options = new TemporaryTopicOptions();
            configureOptions?.Invoke(options);

            CreateTopicOptions createOptions = options.OnSetup.CreateTopicOptions(topicName);

            if (await adminClient.TopicExistsAsync(createOptions.Name))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", createOptions.Name, messagingClient.FullyQualifiedNamespace);
                var topic = new TemporaryTopic(adminClient, messagingClient, messagingClientCreatedByUs, createOptions.Name, topicCreatedByUs: false, options, logger);

                await topic.CleanOnSetupAsync();
                return topic;
            }
            else
            {
                logger.LogTrace("[Test:Setup] Create new Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", createOptions.Name, messagingClient.FullyQualifiedNamespace);
                await adminClient.CreateTopicAsync(createOptions);

                var topic = new TemporaryTopic(adminClient, messagingClient, messagingClientCreatedByUs, createOptions.Name, topicCreatedByUs: true, options, logger);
                await topic.CleanOnSetupAsync();

                return topic;
            }
        }

        private async Task CleanOnSetupAsync()
        {
            if (_options.OnSetup.Messages is OnSetupMessagesTopic.LeaveExistingMessages)
            {
                return;
            }

            await ForEachMessageOnTopicAsync(async (receiver, message) =>
            {
                MessageSettle settle = _options.OnSetup.DetermineMessageSettle(message);

                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Setup] Dead-letter Azure Service bus message '{MessageId}' from topic subscription '{TopicSubscriptionName}' in namespace '{Namespace}'", message.MessageId, receiver.EntityPath, FullyQualifiedNamespace);
                    await receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Setup] Complete Azure Service bus message '{MessageId}' from topic subscription '{TopicSubscriptionName}' in namespace '{Namespace}'", message.MessageId, receiver.EntityPath, FullyQualifiedNamespace);
                    await receiver.CompleteMessageAsync(message);
                }
            });
        }

        /// <summary>
        /// Adds a subscription to this temporary Azure Service Bus topic which will be deleted together with the test fixture.
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription within the topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> is blank.</exception>
        public async Task AddSubscriptionAsync(string subscriptionName)
        {
            await AddSubscriptionAsync(subscriptionName, configureOptions: null);
        }

        /// <summary>
        /// Adds a subscription to this temporary Azure Service Bus topic which will be deleted together with the test fixture.
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription within the topic.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> is blank.</exception>
        public async Task AddSubscriptionAsync(string subscriptionName, Action<TemporaryTopicSubscriptionOptions> configureOptions)
        {
            _subscriptions.Add(await TemporaryTopicSubscription.CreateIfNotExistsAsync(_adminClient, Name, subscriptionName, _logger, configureOptions));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (await _adminClient.TopicExistsAsync(Name))
            {
                if (_topicCreatedByUs)
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", Name, FullyQualifiedNamespace);
                        await _adminClient.DeleteTopicAsync(Name);
                    }));
                }
                else
                {
                    disposables.AddRange(_subscriptions);
                    disposables.Add(AsyncDisposable.Create(CleanOnTeardownAsync));
                }
            }

            if (_messagingClientCreatedByUs)
            {
                disposables.Add(_messagingClient);
            }

            GC.SuppressFinalize(this);
        }

        private async Task CleanOnTeardownAsync()
        {
            await ForEachMessageOnTopicAsync(async (receiver, message) =>
            {
                MessageSettle settle = _options.OnTeardown.DetermineMessageSettle(message, receiver, _logger);

                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Teardown] Dead-letter Azure Service bus message '{MessageId}' from topic subscription '{TopicSubscriptionName}' in namespace '{Namespace}'", message.MessageId, receiver.EntityPath, FullyQualifiedNamespace);
                    await receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Teardown] Complete Azure Service bus message '{MessageId}' from topic subscription '{TopicSubscriptionName}' in namespace '{Namespace}'", message.MessageId, receiver.EntityPath, FullyQualifiedNamespace);
                    await receiver.CompleteMessageAsync(message);
                }
            });
        }

        private async Task ForEachMessageOnTopicAsync(Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> operation)
        {
            await foreach (SubscriptionProperties subscription in _adminClient.GetSubscriptionsAsync(Name))
            {
                await ForEachMessageOnTopicSubscriptionAsync(subscription.SubscriptionName, operation);
            }
        }

        private async Task ForEachMessageOnTopicSubscriptionAsync(string subscriptionName, Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> operation)
        {
            await using ServiceBusReceiver receiver = _messagingClient.CreateReceiver(Name, subscriptionName);

            while (true)
            {
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(_options.OnSetup.MaxWaitTime);
                if (message is null)
                {
                    return;
                }

                await operation(receiver, message);
            }
        }
    }
}
