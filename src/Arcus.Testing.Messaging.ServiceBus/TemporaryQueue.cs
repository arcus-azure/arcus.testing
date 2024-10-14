using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing.Messaging.ServiceBus
{
    /// <summary>
    /// Represents a temporary Azure Service Bus queue that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryQueue : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly string _serviceBusNamespace;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryQueue(
            ServiceBusAdministrationClient client,
            string serviceBusNamespace,
            string queueName,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
            _serviceBusNamespace = serviceBusNamespace;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = queueName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus queue that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string queueName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> or the <paramref name="queueName"/> is blank.
        /// </exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string queueName,
            ILogger logger,
            Action<CreateQueueOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank fully-qualified Azure Service bus namespace to set up a temporary queue", nameof(fullyQualifiedNamespace));
            }

            var client = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(client, queueName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(ServiceBusAdministrationClient adminClient, string queueName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, queueName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryQueue"/> which creates a new Azure Service Bus queue if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic should be created.</param>
        /// <param name="queueName">The name of the Azure Service Bus queue that should be created.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus queue.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus queue should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static async Task<TemporaryQueue> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string queueName,
            ILogger logger,
            Action<CreateQueueOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus queue name to set up a temporary queue", nameof(queueName));
            }

            var options = new CreateQueueOptions(queueName);
            configureOptions?.Invoke(options);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (await adminClient.QueueExistsAsync(options.Name))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus queue '{QueueName}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
                return new TemporaryQueue(adminClient, serviceBusNamespace, options.Name, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus queue '{Queue}' in namespace '{Namespace}'", options.Name, serviceBusNamespace);
            await adminClient.CreateQueueAsync(options);

            return new TemporaryQueue(adminClient, serviceBusNamespace, options.Name, createdByUs: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_createdByUs && await _client.QueueExistsAsync(Name))
            {
                _logger.LogTrace("[Test:Teardown] Delete Azure Service Bus queue '{QueueName}' in namespace '{Namespace}'", Name, _serviceBusNamespace);
                await _client.DeleteQueueAsync(Name);
            }

            GC.SuppressFinalize(this);
        }
    }
}
