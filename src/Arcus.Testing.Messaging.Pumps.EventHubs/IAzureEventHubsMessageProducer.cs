using System.Threading.Tasks;
using Azure.Messaging.EventHubs;

namespace Arcus.Testing.Messaging.Pumps.EventHubs
{
    /// <summary>
    /// Represents a message producer that produces Azure EventHubs messages like it would come from an actual Azure resource.
    /// </summary>
    public interface IAzureEventHubsMessageProducer
    {
        /// <summary>
        /// Produce an Azure EventHubs message like it would come from an actual EventHubs resource.
        /// </summary>
        Task<EventData[]> ProduceMessagesAsync();
    }
}