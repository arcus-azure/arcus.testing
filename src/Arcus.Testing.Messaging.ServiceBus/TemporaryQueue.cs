using System;
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
    internal enum OnSetupQueue { LeaveExistingMessages = 0, DeadLetterMessages, CompleteMessages }

    internal enum OnTeardownQueue { DeadLetterMessages = 0, CompleteMessages = 1 }

    internal enum MessageSettle { DeadLetter, Complete }

    /// <summary>
    /// Represents the available options when setting up the <see cref="TemporaryQueue"/>.
    /// </summary>
    public class OnSetupTemporaryQueueOptions
    {
        private readonly Collection<Action<CreateQueueOptions>> _configuredOptions = new();
        private readonly Collection<Func<ServiceBusReceivedMessage, bool>> _shouldDeadLetterMessages = new(), _shouldCompleteMessages = new();

        internal OnSetupQueue Messages { get; private set; }
        internal TimeSpan MaxWaitTime { get; private set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Configures the <see cref="Azure.Messaging.ServiceBus.Administration.CreateQueueOptions"/> used when the test fixture creates the queue.
        /// </summary>
        /// <remarks>
        ///     Multiple calls gets aggregated together.
        /// </remarks>
        /// <param name="configureOptions">The custom function to alter the way the queue gets created.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureOptions"/> is <c>null</c>.</exception>
        public OnSetupTemporaryQueueOptions CreateQueueWith(Action<CreateQueueOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);

            _configuredOptions.Add(configureOptions);
            return this;
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryQueue"/> to leave any existing messages be on the queue.
        /// </summary>
        public OnSetupTemporaryQueueOptions LeaveExistingMessages()
        {
            Messages = OnSetupQueue.LeaveExistingMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        public OnSetupTemporaryQueueOptions DeadLetterMessages()
        {
            return DeadLetterMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnSetupTemporaryQueueOptions DeadLetterMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
            Messages = OnSetupQueue.DeadLetterMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any pre-existing messages
        /// that matches the given <paramref name="messageFilter"/> on the queue upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="DeadLetterMessages(TimeSpan)"/>.
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be dead-lettered or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnSetupTemporaryQueueOptions DeadLetterMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldDeadLetterMessages.Add(messageFilter);

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///     Can be used in combination with message-filter on-setup methods.
        /// </remarks>
        public OnSetupTemporaryQueueOptions CompleteMessages()
        {
            return CompleteMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Can be used in combination with message-filter on-setup methods.
        ///   </para>
        ///   <para>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="CompleteMessages(TimeSpan)"/>.
        ///   </para>
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnSetupTemporaryQueueOptions CompleteMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
            Messages = OnSetupQueue.CompleteMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any pre-existing messages
        /// that matches the given <paramref name="messageFilter"/> on the queue upon the creation of the test fixture.
        /// </summary>
        /// <param name="messageFilter">The custom filter to determine whether a message should be completed or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnSetupTemporaryQueueOptions CompleteMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldCompleteMessages.Add(messageFilter);

            return this;
        }

        internal CreateQueueOptions CreateQueueOptions(string name)
        {
            var options = new CreateQueueOptions(name);
            foreach (var action in _configuredOptions)
            {
                action(options);
            }

            return options;
        }

        internal MessageSettle DetermineMessageSettle(ServiceBusReceivedMessage message)
        {
            if (_shouldCompleteMessages.Any(func => func(message)))
            {
                return MessageSettle.Complete;
            }

            if (_shouldDeadLetterMessages.Any(func => func(message)))
            {
                return MessageSettle.DeadLetter;
            }

            if (Messages is OnSetupQueue.CompleteMessages)
            {
                return MessageSettle.Complete;
            }

            return MessageSettle.DeadLetter;
        }
    }

    /// <summary>
    /// Represents the available options when tearing down the <see cref="TemporaryQueue"/>.
    /// </summary>
    public class OnTeardownTemporaryQueueOptions
    {
        private readonly Collection<Func<ServiceBusReceivedMessage, bool>> _shouldDeadLetterMessages = new(), _shouldCompleteMessages = new();

        private OnTeardownQueue Messages { get; set; }

        internal TimeSpan MaxWaitTime { get; private set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// (default) Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        public OnTeardownTemporaryQueueOptions DeadLetterMessages()
        {
            return DeadLetterMessages(MaxWaitTime);
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnTeardownTemporaryQueueOptions DeadLetterMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
            Messages = OnTeardownQueue.DeadLetterMessages;

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the queue
        /// that matches the given <paramref name="messageFilter"/>.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="DeadLetterMessages(TimeSpan)"/>.
        ///   </para>
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be dead-lettered or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnTeardownTemporaryQueueOptions DeadLetterMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldDeadLetterMessages.Add(messageFilter);

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        public OnTeardownTemporaryQueueOptions CompleteMessages()
        {
            return CompleteMessages(MaxWaitTime);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     Can be used in combination of message-filter on-teardown methods.
        ///   </para>
        /// </remarks>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnTeardownTemporaryQueueOptions CompleteMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
            Messages = OnTeardownQueue.CompleteMessages;
            
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        ///   </para>
        ///   <para>
        ///     The maximum time to wait for messages on receiving is determined by <see cref="CompleteMessages(TimeSpan)"/>.
        ///   </para>
        /// </remarks>
        /// <param name="messageFilter">The custom filter to determine whether a message should be completed or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnTeardownTemporaryQueueOptions CompleteMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
        {
            ArgumentNullException.ThrowIfNull(messageFilter);
            _shouldCompleteMessages.Add(messageFilter);

            return this;
        }

        internal MessageSettle DetermineMessageSettle(ServiceBusReceivedMessage message)
        {
            if (_shouldCompleteMessages.Any(func => func(message)))
            {
                return MessageSettle.Complete;
            }

            if (_shouldDeadLetterMessages.Any(func => func(message)))
            {
                return MessageSettle.DeadLetter;
            }

            if (Messages is OnTeardownQueue.CompleteMessages)
            {
                return MessageSettle.Complete;
            }

            return MessageSettle.DeadLetter;
        }
    }

    /// <summary>
    /// Represents the available options on the <see cref="TemporaryQueue"/>.
    /// </summary>
    public class TemporaryQueueOptions
    {
        /// <summary>
        /// Gets the options related to setting up the <see cref="TemporaryQueue"/>.
        /// </summary>
        public OnSetupTemporaryQueueOptions OnSetup { get; } = new OnSetupTemporaryQueueOptions().LeaveExistingMessages();

        /// <summary>
        /// Gets the options related to tearing down the <see cref="TemporaryQueue"/>.
        /// </summary>
        public OnTeardownTemporaryQueueOptions OnTeardown { get; } = new OnTeardownTemporaryQueueOptions().DeadLetterMessages();
    }

    /// <summary>
    /// Represents a temporary Azure Service bus queue that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryQueue : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly ServiceBusClient _messagingClient;

        private readonly bool _queueCreatedByUs, _messagingClientCreatedByUs;
        private readonly TemporaryQueueOptions _options;
        private readonly ILogger _logger;

        private TemporaryQueue(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string queueName,
            bool queueCreatedByUs,
            bool messagingClientCreatedByUs,
            TemporaryQueueOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentNullException.ThrowIfNull(messagingClient);
            ArgumentNullException.ThrowIfNull(queueName);

            _adminClient = adminClient;
            _messagingClient = messagingClient;
            _messagingClientCreatedByUs = messagingClientCreatedByUs;
            
            _queueCreatedByUs = queueCreatedByUs;
            
            _options = options ?? new TemporaryQueueOptions();
            _logger = logger ?? NullLogger.Instance;

            Name = queueName;
            FullyQualifiedNamespace = messagingClient.FullyQualifiedNamespace;
        }

        /// <summary>
        /// Gets the name of the Azure Service bus queue that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the fully-qualified name of the Azure Service bus namespace for which this test fixture managed a queue.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Gets the options related to tearing down the <see cref="TemporaryQueue"/>.
        /// </summary>
        public OnTeardownTemporaryQueueOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Gets the filter client to search for messages on the Azure Service bus test-managed queue (a.k.a. 'spy test fixture').
        /// </summary>
        public ServiceBusMessageFilter Messages => new(Name, _messagingClient);

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus queue.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string queueName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string queueName,
            ILogger logger,
            Action<TemporaryQueueOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank fully-qualified Azure Service bus namespace to set up a temporary queue", nameof(fullyQualifiedNamespace));
            }

            var credential = new DefaultAzureCredential();
            var adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);
            var messagingClient = new ServiceBusClient(fullyQualifiedNamespace, credential);
            
            return await CreateIfNotExistsAsync(adminClient, messagingClient, messagingClientCreatedByUs: true, queueName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service bus resource where the topic should be created.</param>
        /// <param name="messagingClient">
        ///     The messaging client to both send and receive messages on the Azure Service bus, as well as handling setup and teardown actions.
        /// </param>
        /// <param name="queueName">The name of the Azure Service bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus queue.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="adminClient"/> or the <paramref name="messagingClient"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string queueName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, messagingClient, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service bus resource where the topic should be created.</param>
        /// <param name="messagingClient">
        ///     The messaging client to both send and receive messages on the Azure Service bus, as well as handling setup and teardown actions.
        /// </param>
        /// <param name="queueName">The name of the Azure Service bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="adminClient"/> or the <paramref name="messagingClient"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string queueName,
            ILogger logger,
            Action<TemporaryQueueOptions> configureOptions)
        {
            return await CreateIfNotExistsAsync(adminClient, messagingClient, messagingClientCreatedByUs: false, queueName, logger, configureOptions);
        }

        private static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            bool messagingClientCreatedByUs,
            string queueName,
            ILogger logger,
            Action<TemporaryQueueOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus queue name to set up a temporary queue", nameof(queueName));
            }

            var options = new TemporaryQueueOptions();
            configureOptions?.Invoke(options);

            CreateQueueOptions createOptions = options.OnSetup.CreateQueueOptions(queueName);

            if (await adminClient.QueueExistsAsync(createOptions.Name))
            {
                logger.LogDebug("[Test:Setup] Use already existing Azure Service bus queue '{QueueName}' in namespace '{Namespace}'", createOptions.Name, messagingClient.FullyQualifiedNamespace);
                var queue = new TemporaryQueue(adminClient, messagingClient, createOptions.Name, queueCreatedByUs: false, messagingClientCreatedByUs, options, logger);

                await queue.CleanOnSetupAsync();
                return queue;
            }
            else
            {
                logger.LogDebug("[Test:Setup] Create new Azure Service bus queue '{Queue}' in namespace '{Namespace}'", createOptions.Name, messagingClient.FullyQualifiedNamespace);
                await adminClient.CreateQueueAsync(createOptions);

                var queue = new TemporaryQueue(adminClient, messagingClient, createOptions.Name, queueCreatedByUs: true, messagingClientCreatedByUs, options, logger);

                await queue.CleanOnSetupAsync();
                return queue;
            }
        }

        private async Task CleanOnSetupAsync()
        {
            if (_options.OnSetup.Messages is OnSetupQueue.LeaveExistingMessages)
            {
                return;
            }

            TimeSpan maxWaitTime = _options.OnSetup.MaxWaitTime;
            await ForEachMessageOnQueueAsync(maxWaitTime, async (receiver, message) =>
            {
                MessageSettle settle = _options.OnSetup.DetermineMessageSettle(message);
                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Setup] Dead-letter Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, FullyQualifiedNamespace);
                    await receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Setup] Complete Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, FullyQualifiedNamespace);
                    await receiver.CompleteMessageAsync(message);
                }
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (await _adminClient.QueueExistsAsync(Name))
            {
                if (_queueCreatedByUs)
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogDebug("[Test:Teardown] Delete Azure Service bus queue '{QueueName}' in namespace '{Namespace}'", Name, FullyQualifiedNamespace);
                        await _adminClient.DeleteQueueAsync(Name);
                    }));
                }
                else
                {
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
            TimeSpan maxWaitTime = _options.OnTeardown.MaxWaitTime;

            await ForEachMessageOnQueueAsync(maxWaitTime, async (receiver, message) =>
            {
                MessageSettle settle = _options.OnTeardown.DetermineMessageSettle(message);
                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Teardown] Dead-letter Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, FullyQualifiedNamespace);
                    await receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Teardown] Complete Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, FullyQualifiedNamespace);
                    await receiver.CompleteMessageAsync(message);
                }
            });
        }

        private async Task ForEachMessageOnQueueAsync(TimeSpan waitTime, Func<ServiceBusReceiver, ServiceBusReceivedMessage, Task> operation)
        {
            await using ServiceBusReceiver receiver = _messagingClient.CreateReceiver(Name);

            while (true)
            {
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(waitTime);
                if (message is null)
                {
                    return;
                }

                await operation(receiver, message);
            }
        }
    }
}
