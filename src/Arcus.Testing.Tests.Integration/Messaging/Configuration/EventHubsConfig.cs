using System;
using Azure.Core;
using Azure.ResourceManager.EventHubs;

namespace Arcus.Testing.Tests.Integration.Messaging.Configuration
{
    /// <summary>
    /// Represents a test configuration subsection where all Azure Event Hubs-related configuration is stored.
    /// </summary>
    public class EventHubsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsConfig" /> class.
        /// </summary>
        public EventHubsConfig(ResourceIdentifier namespaceResourceId)
        {
            ArgumentNullException.ThrowIfNull(namespaceResourceId);
            NamespaceResourceId = namespaceResourceId;
        }

        /// <summary>
        /// Gets the resource ID of the configured Azure Event Hubs namespace.
        /// </summary>
        public ResourceIdentifier NamespaceResourceId { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> to make retrieval more test-friendly.
    /// </summary>
    public static class TestConfigExtensions
    {
        /// <summary>
        /// Loads the <see cref="EventHubsConfig"/> configuration subsection from the current test <paramref name="config"/>.
        /// </summary>
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
