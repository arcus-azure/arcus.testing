using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs.Consumer;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options for the <see cref="TemporaryEventHub"/> when the test fixture is set up.
    /// </summary>
    public class OnSetupTemporaryEventHubOptions
    {
        internal EventHubData EventHubData { get; } = new();

        /// <summary>
        /// Configures the <see cref="Azure.ResourceManager.EventHubs.EventHubData"/> when an Azure EvenHubs hub is being created.
        /// </summary>
        /// <param name="configureHub">The additional function to manipulate how the Azure EventHubs hub is created by the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureHub"/> is <c>null</c>.</exception>
        public OnSetupTemporaryEventHubOptions CreateHubWith(Action<EventHubData> configureHub)
        {
            ArgumentNullException.ThrowIfNull(configureHub);

            configureHub(EventHubData);
            return this;
        }
    }

    /// <summary>
    /// Represents the available options for the <see cref="TemporaryEventHub"/> when the test fixture is teardown.
    /// </summary>
    public class OnTeardownTemporaryEventHubOptions
    {

    }

    /// <summary>
    /// Represents the available options for the <see cref="TemporaryEventHub"/>.
    /// </summary>
    public class TemporaryEventHubOptions
    {
        /// <summary>
        /// Gets the available options when the test fixture is set up.
        /// </summary>
        public OnSetupTemporaryEventHubOptions OnSetup { get; } = new();

        /// <summary>
        /// Gets the available options when the test fixture is teardown.
        /// </summary>
        public OnTeardownTemporaryEventHubOptions OnTeardown { get; } = new();
    }

    /// <summary>
    /// Represents a temporary Azure EventHubs hub that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryEventHub : IAsyncDisposable
    {
        private readonly EventHubsNamespaceResource _eventHubsNamespace;
        private readonly EventHubConsumerClient _consumerClient;
        private readonly string _eventHubName;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryEventHub(
            EventHubsNamespaceResource eventHubsNamespace,
            EventHubConsumerClient consumerClient,
            string eventHubName,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(eventHubsNamespace);
            ArgumentNullException.ThrowIfNull(consumerClient);

            _eventHubsNamespace = eventHubsNamespace;
            _consumerClient = consumerClient;
            _eventHubName = eventHubName;
            _createdByUs = createdByUs;
            _logger = logger;
        }

        /// <summary>
        /// Gets the filter client to search for events on the Azure EventHubs test-managed hub (a.k.a. 'spy test fixture').
        /// </summary>
        public EventHubEventFilter Events => new(_consumerClient);

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEventHub"/> which creates a new Azure EventHubs hub if it doesn't exist yet.
        /// </summary>
        /// <param name="eventHubNamespaceResourceId"></param>
        /// <param name="consumerGroup"></param>
        /// <param name="eventHubName"></param>
        /// <param name="logger"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<TemporaryEventHub> CreateIfNotExistsAsync(
            ResourceIdentifier eventHubNamespaceResourceId,
            string consumerGroup,
            string eventHubName,
            ILogger logger,
            Action<TemporaryEventHubOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(eventHubNamespaceResourceId);

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Requires a non-blank Azure EventHubs consumer group to set up the test fixture", nameof(consumerGroup));
            }

            var credential = new DefaultAzureCredential();
            var arm = new ArmClient(credential);

            EventHubsNamespaceResource resource =
                await arm.GetEventHubsNamespaceResource(eventHubNamespaceResourceId)
                         .GetAsync();

            var consumerClient = new EventHubConsumerClient(consumerGroup, resource.Data.ServiceBusEndpoint, eventHubName, credential);

            return await CreateIfNotExistsAsync(resource, consumerClient, eventHubName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEventHub"/> which creates a new Azure EventHubs hub if it doesn't exist yet.
        /// </summary>
        /// <param name="eventHubsNamespace"></param>
        /// <param name="consumerClient"></param>
        /// <param name="eventHubName"></param>
        /// <param name="logger"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<TemporaryEventHub> CreateIfNotExistsAsync(
            EventHubsNamespaceResource eventHubsNamespace,
            EventHubConsumerClient consumerClient,
            string eventHubName,
            ILogger logger,
            Action<TemporaryEventHubOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(eventHubsNamespace);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(eventHubName))
            {
                throw new ArgumentException("Requires a non-blank Azure EventHubs hub name to set up the test fixture", nameof(eventHubName));
            }

            var options = new TemporaryEventHubOptions();
            configureOptions?.Invoke(options);

            EventHubCollection eventHubs = eventHubsNamespace.GetEventHubs();
            if (await eventHubs.ExistsAsync(eventHubName))
            {
                logger.LogDebug("[Test:Setup] Use already existing Azure EventHubs hub '{EventHubName}' in namespace '{Namespace}'", eventHubName, eventHubsNamespace.Id.Name);

                return new TemporaryEventHub(eventHubsNamespace, consumerClient, eventHubName, createdByUs: false, logger);
            }

            logger.LogDebug("[Test:Setup] Create new Azure EventHubs hub '{EventHubName}' in namespace '{Namespace}'", eventHubName, eventHubsNamespace.Id.Name);
            await eventHubs.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, options.OnSetup.EventHubData);

            return new TemporaryEventHub(eventHubsNamespace, consumerClient, eventHubName, createdByUs: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (_createdByUs)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    NullableResponse<EventHubResource> eventHub =
                        await _eventHubsNamespace.GetEventHubs()
                                                 .GetIfExistsAsync(_eventHubName);

                    if (eventHub.HasValue && eventHub.Value != null)
                    {
                        _logger.LogDebug("[Test:Teardown] Delete Azure EventHubs hub '{EventHubName}' in namespace '{Namespace}'", _eventHubName, _eventHubsNamespace.Id.Name);
                        await eventHub.Value.DeleteAsync(WaitUntil.Completed);
                    }
                }));
            }

            disposables.Add(_consumerClient);
        }
    }
}
