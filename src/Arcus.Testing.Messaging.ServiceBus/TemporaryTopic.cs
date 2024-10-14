using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing.Messaging.ServiceBus
{
    /// <summary>
    /// Represents a temporary Azure Service Bus topic that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopic : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly Collection<TemporaryTopicSubscription> _subscriptions = new();
        private readonly string _serviceBusNamespace;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryTopic(
            ServiceBusAdministrationClient client,
            string serviceBusNamespace,
            string topicName,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
            _serviceBusNamespace = serviceBusNamespace;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = topicName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

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
            Action<CreateTopicOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank fully-qualified Azure Service bus namespace to create a temporary topic", nameof(fullyQualifiedNamespace));
            }

            var client = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(client, topicName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(ServiceBusAdministrationClient adminClient, string topicName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, topicName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopic"/> which creates a new Azure Service Bus topic if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static async Task<TemporaryTopic> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            ILogger logger,
            Action<CreateTopicOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service Bus topic name to create a temporary topic", nameof(topicName));
            }
            
            var options = new CreateTopicOptions(topicName);
            configureOptions?.Invoke(options);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (await adminClient.TopicExistsAsync(options.Name))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
                return new TemporaryTopic(adminClient, serviceBusNamespace, options.Name, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
            await adminClient.CreateTopicAsync(options);

            return new TemporaryTopic(adminClient, serviceBusNamespace, options.Name, createdByUs: true, logger);
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
        public async Task AddSubscriptionAsync(string subscriptionName, Action<CreateSubscriptionOptions> configureOptions)
        {
            _subscriptions.Add(await TemporaryTopicSubscription.CreateIfNotExistsAsync(_client, Name, subscriptionName, _logger, configureOptions));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.AddRange(_subscriptions);

            if (_createdByUs)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    if (await _client.TopicExistsAsync(Name))
                    {
                        _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus topic '{TopicName}' in namespace '{Namespace}'", Name, _serviceBusNamespace);
                        await _client.DeleteTopicAsync(Name); 
                    }
                }));
            }

            GC.SuppressFinalize(this);
        }
    }
}
