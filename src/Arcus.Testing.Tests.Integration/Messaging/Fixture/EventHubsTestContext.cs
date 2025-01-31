using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Messaging.Fixture
{
    /// <summary>
    /// Represents a test-friendly interaction with Azure EventHubs.
    /// </summary>
    public class EventHubsTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly EventHubsNamespaceResource _namespace;
        private readonly Collection<string> _eventHubNames = new();
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private EventHubsTestContext(
            TemporaryManagedIdentityConnection connection,
            EventHubsNamespaceResource @namespace,
            ILogger logger)
        {
            _connection = connection;
            _namespace = @namespace;
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="EventHubsTestContext"/> based on the current test <paramref name="config"/>.
        /// </summary>
        public static async Task<EventHubsTestContext> GivenAsync(TestConfig config, ILogger logger)
        {
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            EventHubsConfig eventHubs = config.GetEventHubs();

            var connection = TemporaryManagedIdentityConnection.Create(servicePrincipal);
            var credential = new DefaultAzureCredential();

            var arm = new ArmClient(credential);
            EventHubsNamespaceResource resource =
                await arm.GetEventHubsNamespaceResource(eventHubs.NamespaceResourceId)
                         .GetAsync();

            return new EventHubsTestContext(connection, resource, logger);
        }

        /// <summary>
        /// Sets up an existing Azure Event Hub.
        /// </summary>
        /// <returns>The name of the newly set up Azure Event Hub.</returns>
        public async Task<string> WhenHubAvailableAsync()
        {
            string eventHubName = WhenHubNonAvailable();

            _logger.LogDebug("[Test:Setup] Create existing Azure EventHbs '{EventHubName}' in namespace '{Namespace}'", eventHubName, _namespace.Id.Name);

            var eventHubs = _namespace.GetEventHubs();
            await eventHubs.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, new EventHubData
            {
                PartitionCount = 1,
                RetentionDescription = new RetentionDescription
                {
                    CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
                    RetentionTimeInHours = 1
                }
            });

            return eventHubName;
        }

        /// <summary>
        /// Sets up a non-existing Azure Event Hub.
        /// </summary>
        /// <returns>The name of the non-existing Azure Event Hub.</returns>
        public string WhenHubNonAvailable()
        {
            string eventHubName = $"hub-{Bogus.Random.Guid()}";
            _eventHubNames.Add(eventHubName);

            return eventHubName;
        }

        /// <summary>
        /// Place an event on an Azure Event Hub.
        /// </summary>
        /// <param name="eventHubName">The name of the hub where to place the event.</param>
        /// <param name="partitionId">The optional partition ID where specifically the event should be placed.</param>
        public async Task<EventData> WhenEventAvailableOnHubAsync(string eventHubName, string partitionId = null)
        {
            var producerClient = new EventHubProducerClient(_namespace.Data.ServiceBusEndpoint, eventHubName, new DefaultAzureCredential());

            var ev = new EventData(Bogus.Random.Bytes(10))
            {
                MessageId = $"id-{Bogus.Random.Guid()}"
            };

            _logger.LogDebug("[Test:Setup] Send event '{MessageId}' on Azure EventHubs hub '{EventHubName}' in namespace '{Namespace}'", ev.MessageId, eventHubName, _namespace.Id.Name);
            await producerClient.SendAsync([ev], new SendEventOptions() { PartitionId = partitionId });

            return ev;
        }

        /// <summary>
        /// Verifies that there exists an Azure Event Hub with the given <paramref name="eventHubName"/> in the currently configured namespace.
        /// </summary>
        public async Task ShouldHaveHubAsync(string eventHubName)
        {
            Assert.True(await _namespace.GetEventHubs().ExistsAsync(eventHubName), $"Azure EventHubs hub '{eventHubName}' should be available on the namespace, but it isn't");
        }

        /// <summary>
        /// Verifies that there does not exist an Azure Event Hub with the given <paramref name="eventHubName"/> in the currently configured namespace.
        /// </summary>
        public async Task ShouldNotHaveHubAsync(string eventHubName)
        {
            Assert.False(await _namespace.GetEventHubs().ExistsAsync(eventHubName), $"Azure EventHubs hub '{eventHubName}' should not be available on the namespace, but it is");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (_eventHubNames.Count > 0)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    EventHubCollection eventHubs = _namespace.GetEventHubs();
                    foreach (var eventHubName in _eventHubNames)
                    {
                        NullableResponse<EventHubResource> eventHub = await eventHubs.GetIfExistsAsync(eventHubName);
                        if (eventHub.HasValue && eventHub.Value != null)
                        {
                            _logger.LogDebug("[Test:Teardown] Fallback delete Azure EventHubs hub '{EventHubName}' in namespace '{Namespace}'", eventHubName, _namespace.Id.Name);
                            await eventHub.Value.DeleteAsync(WaitUntil.Started);
                        }
                    }
                }));
            }

            disposables.Add(_connection);

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Extensions on the <see cref="EventHubEventFilter"/> to make interaction more test-friendly.
    /// </summary>
    public static class EventHubEventFilterExtensions
    {
        /// <summary>
        /// Verifies that the configured <paramref name="filter"/> indeed only found a single event.
        /// </summary>
        public static async Task ShouldHaveSingleAsync(this EventHubEventFilter filter)
        {
            List<PartitionEvent> events = await filter.ReadWith(opt => opt.MaximumWaitTime = TimeSpan.FromSeconds(10)).ToListAsync();
            Assert.True(events.Count == 1, $"Azure EventHubs hub should have a single event available, but there were '{events.Count}' events");
        }
    }
}
