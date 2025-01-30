using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Messaging.Configuration;
using Arcus.Testing.Tests.Integration.Messaging.Fixture;
using Azure.Messaging.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Xunit;
using Xunit.Abstractions;

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
            Assert.Single(await temp.Events.Where(ev => ev.Data.MessageId == expected.MessageId).ToListAsync());

            await temp.DisposeAsync();
            await eventHubs.ShouldHaveHubAsync(eventHubName);
        }

        private async Task<TemporaryEventHub> CreateTempHubAsync(string eventHubName)
        {
            EventHubsConfig config = Configuration.GetEventHubs();

            return await TemporaryEventHub.CreateIfNotExistsAsync(
                config.NamespaceResourceId, "$Default", eventHubName, Logger, options =>
                {
                    options.OnSetup.CreateHubWith(hub =>
                    {
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
