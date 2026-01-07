using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.ResourceManager.EventHubs.Models;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Messaging
{
    public class TemporaryEventHubTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryEventHubTests" /> class.
        /// </summary>
        public TemporaryEventHubTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempHub_WithNonExistingHub_SucceedsByCreatingHubDuringLifetimeFixture()
        {
            // Arrange
            await using var eventHubs = await GivenEventHubNamespaceAsync();

            string eventHubName = eventHubs.WhenHubNonAvailable();

            // Act
            TemporaryEventHub temp = await CreateTempHubAsync(eventHubName);

            // Assert
            await eventHubs.ShouldHaveHubAsync(eventHubName);

            string partitionId = "0";
            EventData expected = await eventHubs.WhenEventAvailableOnHubAsync(eventHubName, partitionId);

            await temp.Events.FromPartition(partitionId, EventPosition.Earliest)
                             .Where(ev => ev.Data.MessageId == expected.MessageId)
                             .ShouldHaveSingleAsync();

            await temp.DisposeAsync();
            await eventHubs.ShouldNotHaveHubAsync(eventHubName);
        }

        [Fact]
        public async Task CreateTempHub_WithExistingHub_SucceedsByLeavingHubAfterLifetimeFixture()
        {
            // Arrange
            await using var eventHubs = await GivenEventHubNamespaceAsync();

            string eventHubName = await eventHubs.WhenHubAvailableAsync();
            EventData expected = await eventHubs.WhenEventAvailableOnHubAsync(eventHubName);

            // Act
            TemporaryEventHub temp = await CreateTempHubAsync(eventHubName);

            // Assert
            await eventHubs.ShouldHaveHubAsync(eventHubName);

            await temp.Events.Where(ev => ev.Data.MessageId == expected.MessageId)
                             .ShouldHaveSingleAsync();

            await temp.DisposeAsync();
            await eventHubs.ShouldHaveHubAsync(eventHubName);
        }

        private async Task<TemporaryEventHub> CreateTempHubAsync(string eventHubName)
        {
            EventHubsConfig config = Configuration.GetEventHubs();

            return await TemporaryEventHub.CreateIfNotExistsAsync(
                config.NamespaceResourceId, consumerGroup: "$Default", eventHubName, Logger, options =>
                {
                    options.OnSetup.CreateHubWith(hub =>
                    {
                        hub.PartitionCount = 1;
                        hub.RetentionDescription = new RetentionDescription
                        {
                            CleanupPolicy = CleanupPolicyRetentionDescription.Delete,
                            RetentionTimeInHours = 1
                        };
                    });
                });
        }

        private async Task<EventHubsTestContext> GivenEventHubNamespaceAsync()
        {
            return await EventHubsTestContext.GivenAsync(Configuration, Logger);
        }
    }
}
