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

        internal Func<string, CreateQueueOptions> CreateQueueOptions => name =>
        {
            var options = new CreateQueueOptions(name);
            foreach (var action in _configuredOptions)
            {
                action(options);
            }

            return options;
        };

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
        /// Configures the maximum time duration to wait for messages when setting up the test fixture.
        /// </summary>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnSetupTemporaryQueueOptions WaitMaxForMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
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
        public OnSetupTemporaryQueueOptions DeadLetterMessages()
        {
            Messages = OnSetupQueue.DeadLetterMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
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
        public OnSetupTemporaryQueueOptions CompleteMessages()
        {
            Messages = OnSetupQueue.CompleteMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any pre-existing messages on the queue upon the creation of the test fixture.
        /// </summary>
        /// <param name="messageFilter">The custom filter to determine whether a message should be completed or not.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageFilter"/> is <c>null</c>.</exception>
        public OnSetupTemporaryQueueOptions CompleteMessages(Func<ServiceBusReceivedMessage, bool> messageFilter)
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
        /// Configures the maximum time duration to wait for messages when tearing down the test fixture.
        /// </summary>
        /// <param name="maxWaitTime">The maximum time to wait for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxWaitTime"/> is a negative duration.</exception>
        public OnTeardownTemporaryQueueOptions WaitMaxForMessages(TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime), maxWaitTime, "Requires positive time duration to represent the maximum wait time for messages");
            }

            MaxWaitTime = maxWaitTime;
            return this;
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        /// </remarks>
        public OnTeardownTemporaryQueueOptions DeadLetterMessages()
        {
            Messages = OnTeardownQueue.DeadLetterMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to dead-letter any remaining messages on the queue
        /// that matches the given <paramref name="messageFilter"/>.
        /// </summary>
        /// <remarks>
        ///     This both dead-letters messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
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
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
        /// </remarks>
        public OnTeardownTemporaryQueueOptions CompleteMessages()
        {
            Messages = OnTeardownQueue.CompleteMessages;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryQueue"/> to complete any remaining messages on the queue.
        /// </summary>
        /// <remarks>
        ///     This both completes messages that were send via the test fixture and outside the test fixture
        ///     to ensure a non-lingering queue after the test run.
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

        private readonly string _serviceBusNamespace;
        private readonly bool _createdByUs;
        private readonly TemporaryQueueOptions _options;
        private readonly ILogger _logger;

        private TemporaryQueue(
            ServiceBusAdministrationClient adminClient,
            ServiceBusClient messagingClient,
            string serviceBusNamespace,
            string queueName,
            bool createdByUs,
            TemporaryQueueOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentNullException.ThrowIfNull(messagingClient);
            ArgumentNullException.ThrowIfNull(serviceBusNamespace);
            ArgumentNullException.ThrowIfNull(queueName);

            _adminClient = adminClient;
            _messagingClient = messagingClient;
            _serviceBusNamespace = serviceBusNamespace;
            _createdByUs = createdByUs;
            _options = options ?? new TemporaryQueueOptions();
            _logger = logger ?? NullLogger.Instance;

            Name = queueName;
            Sender = _messagingClient.CreateSender(queueName);
            Receiver = _messagingClient.CreateReceiver(queueName);
        }

        /// <summary>
        /// Gets the name of the Azure Service bus queue that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the options related to tearing down the <see cref="TemporaryQueue"/>.
        /// </summary>
        public OnTeardownTemporaryQueueOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Gets the client to send messages to this Azure Service bus test-managed queue.
        /// </summary>
        public ServiceBusSender Sender { get; }

        /// <summary>
        /// Gets the client to receive messages from this Azure Service bus test-managed queue.
        /// </summary>
        public ServiceBusReceiver Receiver { get; }

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
            
            return await CreateIfNotExistsAsync(adminClient, messagingClient, queueName, logger, configureOptions);
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
            ArgumentNullException.ThrowIfNull(adminClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus queue name to set up a temporary queue", nameof(queueName));
            }

            var options = new TemporaryQueueOptions();
            configureOptions?.Invoke(options);

            var createOptions = options.OnSetup.CreateQueueOptions(queueName);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (await adminClient.QueueExistsAsync(createOptions.Name))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service bus queue '{QueueName}' in namespace '{Namespace}'", createOptions.Name, serviceBusNamespace);
                var queue = new TemporaryQueue(adminClient, messagingClient, serviceBusNamespace, createOptions.Name, createdByUs: false, options, logger);

                await queue.CleanOnSetupAsync();
                return queue;
            }
            else
            {
                logger.LogTrace("[Test:Setup] Create new Azure Service bus queue '{Queue}' in namespace '{Namespace}'", createOptions.Name, serviceBusNamespace);
                await adminClient.CreateQueueAsync(createOptions);

                var queue = new TemporaryQueue(adminClient, messagingClient, serviceBusNamespace, createOptions.Name, createdByUs: true, options, logger);

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
            await ForEachMessageOnQueueAsync(maxWaitTime, async message =>
            {
                MessageSettle settle = _options.OnSetup.DetermineMessageSettle(message);
                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Setup] Dead-letter Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, _serviceBusNamespace);
                    await Receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Setup] Complete Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, _serviceBusNamespace);
                    await Receiver.CompleteMessageAsync(message);
                }
            });
        }

        /// <summary>
        /// Sends a message to the temporary available Azure Service bus queue.
        /// </summary>
        /// <param name="message">The message to send to the queue.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> or its ID are <c>null</c>.</exception>
        public async Task SendMessageAsync(ServiceBusMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            await Sender.SendMessageAsync(message);
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
                if (_createdByUs)
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogDebug("[Test:Teardown] Delete Azure Service bus queue '{QueueName}' in namespace '{Namespace}'", Name, _serviceBusNamespace);
                        await _adminClient.DeleteQueueAsync(Name);
                    }));
                }
                else
                {
                    disposables.Add(AsyncDisposable.Create(CleanOnTeardownAsync));
                }
            }

            disposables.Add(Sender);
            disposables.Add(Receiver);
            disposables.Add(_messagingClient);

            GC.SuppressFinalize(this);
        }

        private async Task CleanOnTeardownAsync()
        {
            TimeSpan maxWaitTime = _options.OnTeardown.MaxWaitTime;

            await ForEachMessageOnQueueAsync(maxWaitTime, async message =>
            {
                MessageSettle settle = _options.OnTeardown.DetermineMessageSettle(message);
                if (settle is MessageSettle.DeadLetter)
                {
                    _logger.LogDebug("[Test:Teardown] Dead-letter Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, _serviceBusNamespace);
                    await Receiver.DeadLetterMessageAsync(message);
                }
                else if (settle is MessageSettle.Complete)
                {
                    _logger.LogDebug("[Test:Teardown] Complete Azure Service bus message '{MessageId}' from queue '{QueueName}' in namespace '{Namespace}'", message.MessageId, Name, _serviceBusNamespace);
                    await Receiver.CompleteMessageAsync(message);
                }
            });
        }

        private async Task ForEachMessageOnQueueAsync(TimeSpan waitTime, Func<ServiceBusReceivedMessage, Task> operation)
        {
            while (true)
            {
                ServiceBusReceivedMessage message = await Receiver.ReceiveMessageAsync(waitTime);
                if (message is null)
                {
                    return;
                }

                await operation(message);
            }
        }
    }
}
