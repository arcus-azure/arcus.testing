using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Messaging.Fixture
{
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

        public async Task<string> WhenHubAvailableAsync()
        {
            string eventHubName = WhenHubNonAvailable();

            _logger.LogDebug("[Test:Setup] Create existing Azure EventHbs '{EventHubName}' in namespace '{Namespace}'", eventHubName, _namespace.Id.Name);

            var eventHubs = _namespace.GetEventHubs();
            await eventHubs.CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, new EventHubData
            {
                RetentionDescription = new RetentionDescription
                {
                    CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
                    RetentionTimeInHours = 1
                }
            });

            return eventHubName;
        }

        public string WhenHubNonAvailable()
        {
            string eventHubName = $"hub-{Bogus.Random.Guid()}";
            _eventHubNames.Add(eventHubName);

            return eventHubName;
        }

        public async Task<EventData> WhenEventAvailableOnHubAsync(string eventHubName, string partitionId = null)
        {
            var producerClient = new EventHubProducerClient(_namespace.Data.ServiceBusEndpoint, eventHubName, new DefaultAzureCredential());

            var ev = new EventData(Bogus.Random.Bytes(10))
            {
                MessageId = $"id-{Bogus.Random.Guid()}"
            };
            await producerClient.SendAsync([ev], new SendEventOptions() { PartitionId = partitionId });

            return ev;
        }

        public async Task ShouldHaveHubAsync(string evenHubName)
        {
            Assert.True(await _namespace.GetEventHubs().ExistsAsync(evenHubName), $"Azure EventHubs hub '{evenHubName}' should be available on the namespace, but it isn't");
        }

        public async Task ShouldNotHaveHubAsync(string eventHubName)
        {
            Assert.False(await _namespace.GetEventHubs().ExistsAsync(eventHubName), $"Azure EventHubs hub '{eventHubName}' should not be available on the namespace, but it is");
        }

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
}
