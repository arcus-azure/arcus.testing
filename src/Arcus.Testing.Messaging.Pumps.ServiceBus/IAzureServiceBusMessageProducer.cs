using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Testing.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a message producer that produces Azure Service Bus messages like it would come from an actual Azure resource.
    /// </summary>
    public interface IAzureServiceBusMessageProducer
    {
        /// <summary>
        /// Produce an Azure Service Bus message like it would come from an actual Service Bus resource.
        /// </summary>
        Task<ServiceBusReceivedMessage[]> ProduceMessagesAsync();
    }
}