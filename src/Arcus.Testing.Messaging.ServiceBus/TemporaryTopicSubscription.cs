using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing.Messaging.ServiceBus
{
    /// <summary>
    /// Represents a temporary Azure Service Bus topic subscription that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopicSubscription : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly string _serviceBusNamespace;
        private readonly CreateSubscriptionOptions _options;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryTopicSubscription(
            ServiceBusAdministrationClient client,
            string serviceBusNamespace,
            CreateSubscriptionOptions options,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceBusNamespace);

            _client = client;
            _serviceBusNamespace = serviceBusNamespace;
            _options = options;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = _options.SubscriptionName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic subscription that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/> in the given <paramref name="fullyQualifiedNamespace"/>.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string topicName, string subscriptionName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, subscriptionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/> in the given <paramref name="fullyQualifiedNamespace"/>.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string topicName,
            string subscriptionName,
            ILogger logger,
            Action<CreateSubscriptionOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);

            var client = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(client, topicName, subscriptionName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic subscription should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, topicName, subscriptionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic subscription should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            ILogger logger,
            Action<CreateSubscriptionOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
            logger ??= NullLogger.Instance;

            var options = new CreateSubscriptionOptions(topicName, subscriptionName);
            configureOptions?.Invoke(options);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (!await adminClient.TopicExistsAsync(options.TopicName))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] cannot create temporary subscription '{options.SubscriptionName}' on Azure Service Bus topic '{serviceBusNamespace}/{options.TopicName}' " +
                    $"because the topic '{options.TopicName}' does not exists in the provided Azure Service Bus namespace. " +
                    $"Please make sure to have an available Azure Service Bus topic before using the temporary topic subscription test fixture");
            }

            if (await adminClient.SubscriptionExistsAsync(options.TopicName, options.SubscriptionName))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", options.SubscriptionName, serviceBusNamespace, options.TopicName);
                return new TemporaryTopicSubscription(adminClient, serviceBusNamespace, options, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", options.SubscriptionName, serviceBusNamespace, options.TopicName);
            await adminClient.CreateSubscriptionAsync(options);

            return new TemporaryTopicSubscription(adminClient, serviceBusNamespace, options, createdByUs: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            if (_createdByUs && await _client.SubscriptionExistsAsync(_options.TopicName, _options.SubscriptionName))
            {
                _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", _options.SubscriptionName, _serviceBusNamespace, _options.TopicName);
                await _client.DeleteSubscriptionAsync(_options.TopicName, _options.SubscriptionName);
            }
        }
    }
}
