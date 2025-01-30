using Azure.Core;
using Azure.ResourceManager.EventHubs;

namespace Arcus.Testing.Tests.Integration.Messaging.Configuration
{
    public class EventHubsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        public EventHubsConfig(ResourceIdentifier namespaceResourceId)
        {
            NamespaceResourceId = namespaceResourceId;
        }

        public ResourceIdentifier NamespaceResourceId { get; }
    }

    public static class TestConfigExtensions
    {
        public static EventHubsConfig GetEventHubs(this TestConfig config)
        {
            var resourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
                config["Arcus:SubscriptionId"],
                config["Arcus:ResourceGroup:Name"],
                config["Arcus:EventHubs:Namespace"]);

            return new EventHubsConfig(resourceId);
        }
    }
}
