using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Messaging.Fixture
{
    internal class ServiceBusTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly Collection<string> _topicNames = new(), _queueNames = new();
        private readonly Collection<(string topicName, string subscriptionName)> _subscriptionNames = new();
        private readonly ILogger _logger;

        private ServiceBusTestContext(
            TemporaryManagedIdentityConnection connection,
            ServiceBusAdministrationClient adminClient,
            ILogger logger)
        {
            _connection = connection;
            _adminClient = adminClient;
            _logger = logger;
        }

        public static ServiceBusTestContext Given(TestConfig config, ILogger logger)
        {
            ServicePrincipal servicePrincipal = config.GetServicePrincipal();
            ServiceBusNamespace serviceBus = config.GetServiceBus();

            var connection = TemporaryManagedIdentityConnection.Create(servicePrincipal);
            var client = new ServiceBusAdministrationClient(serviceBus.HostName, new DefaultAzureCredential());

            return new ServiceBusTestContext(connection, client, logger);
        }

        public async Task<string> WhenQueueAvailableAsync()
        {
            string queueName = WhenQueueUnavailable();
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus queue '{QueueName}'", queueName);

            await _adminClient.CreateQueueAsync(queueName);
            return queueName;
        }

        public string WhenQueueUnavailable()
        {
            string queueName = $"queue-{Guid.NewGuid()}";
            _queueNames.Add(queueName);
            
            return queueName;
        }

        public async Task<string> WhenTopicAvailableAsync()
        {
            string topicName = WhenTopicUnavailable();
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus topic '{TopicName}'", topicName);

            await _adminClient.CreateTopicAsync(topicName);
            return topicName;
        }

        public string WhenTopicUnavailable()
        {
            string topicName = $"topic-{Guid.NewGuid()}";
            _topicNames.Add(topicName);

            return topicName;
        }

        public async Task<string> WhenTopicSubscriptionAvailableAsync(string topicName)
        {
            string subscriptionName = WhenTopicSubscriptionUnavailable(topicName);
            _logger.LogTrace("[Test:Setup] Create available Azure Service Bus topic subscription '{SubscriptionName}' on topic '{TopicName}'", subscriptionName, topicName);

            await _adminClient.CreateSubscriptionAsync(topicName, subscriptionName);
            return subscriptionName;
        }

        public string WhenTopicSubscriptionUnavailable(string topicName)
        {
            string subscriptionName = $"sub-{Guid.NewGuid()}";
            _subscriptionNames.Add((topicName, subscriptionName));

            return subscriptionName;
        }

        public async Task WhenQueueDeletedAsync(string queueName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus queue '{QueueName}'", queueName);
            await _adminClient.DeleteQueueAsync(queueName);
        }

        public async Task WhenTopicDeletedAsync(string topicName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus topic '{TopicName}'", topicName);
            await _adminClient.DeleteTopicAsync(topicName);
        }

        public async Task WhenTopicSubscriptionDeletedAsync(string topicName, string subscriptionName)
        {
            _logger.LogTrace("[Test:Setup] Delete available Azure Service Bus topic subscription '{SubscriptionName}' in topic '{TopicName}'", subscriptionName, topicName);
            await _adminClient.DeleteSubscriptionAsync(topicName, subscriptionName);
        }

        public async Task ShouldHaveQueueAsync(string queueName)
        {
            Assert.True(await _adminClient.QueueExistsAsync(queueName), $"Azure Service Bus queue '{queueName}' should be available on the namespace, but it isn't");
        }

        public async Task ShouldNotHaveQueueAsync(string queueName)
        {
            Assert.False(await _adminClient.QueueExistsAsync(queueName), $"Azure Service Bus queue '{queueName}' should not be available on the namespace, but it is");
        }

        public async Task ShouldHaveTopicAsync(string topicName)
        {
            Assert.True(await _adminClient.TopicExistsAsync(topicName), $"Azure Service Bus topic '{topicName}' should be available on the namespace, but it isn't");
        }

        public async Task ShouldNotHaveTopicAsync(string topicName)
        {
            Assert.False(await _adminClient.TopicExistsAsync(topicName), $"Azure Service Bus topic '{topicName}' should not be available on the namespace, but it is");
        }

        public async Task ShouldHaveTopicSubscriptionAsync(string topicName, string subscriptionName)
        {
            Assert.True(await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName), $"Azure Service Bus topic '{topicName}' should have a subscription '{subscriptionName}', but it hasn't");
        }

        public async Task ShouldNotHaveTopicSubscriptionAsync(string topicName, string subscriptionName)
        {
            Assert.False(await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName), $"Azure Service Bus topic '{topicName}' should not have a subscription '{subscriptionName}', but it has");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.AddRange(_queueNames.Select(queueName => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.QueueExistsAsync(queueName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus queue '{QueueName}'", queueName);
                    await _adminClient.DeleteQueueAsync(queueName);
                }
            })));

            disposables.AddRange(_subscriptionNames.Select(item => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.SubscriptionExistsAsync(item.topicName, item.subscriptionName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus topic subscription '{SubscriptionName}' on topic '{TopicName}'", item.subscriptionName, item.topicName);
                    await _adminClient.DeleteSubscriptionAsync(item.topicName, item.subscriptionName);
                }
            })));

            disposables.AddRange(_topicNames.Select(topicName => AsyncDisposable.Create(async () =>
            {
                if (await _adminClient.TopicExistsAsync(topicName))
                {
                    _logger.LogTrace("[Test:Teardown] Fallback delete Azure Service Bus topic '{TopicName}'", topicName);
                    await _adminClient.DeleteTopicAsync(topicName);
                }
            })));

            disposables.Add(_connection);
        }
    }
}
