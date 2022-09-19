using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Newtonsoft.Json;

namespace Arcus.Testing.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a message producer that produces Azure Service Bus messages like it would come from an actual Azure resource.
    /// </summary>
    public class TestAzureServiceBusMessageProducer : IAzureServiceBusMessageProducer
    {
        private readonly ICollection<Func<ServiceBusReceivedMessage[]>> _createMessagesCollection = new Collection<Func<ServiceBusReceivedMessage[]>>();

        /// <summary>
        /// Adds an Azure Service Bus <paramref name="message"/> on the message pump.
        /// </summary>
        /// <param name="message">The message to produce on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public TestAzureServiceBusMessageProducer AddMessage(ServiceBusReceivedMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to produce on the message pump");
            return AddMessages(new[] { message });
        }

        /// <summary>
        /// Adds a series of Azure Service Bus <paramref name="messages"/> on the message pump.
        /// </summary>
        /// <param name="messages">The series of messages to produce on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public TestAzureServiceBusMessageProducer AddMessages(IEnumerable<ServiceBusReceivedMessage> messages)
        {
            Guard.NotNull(messages, nameof(messages), "Requires a series of Azure Service Bus messages to produce on the message pump");

            _createMessagesCollection.Add(messages.ToArray);
            return this;
        }

        /// <summary>
        /// Add a series of custom <paramref name="message"/> which will result in <see cref="ServiceBusReceivedMessage"/> on the message pump.
        /// </summary>
        /// <typeparam name="TMessageBody">The custom type of the message body.</typeparam>
        /// <param name="message">The series of custom message bodies which will translate to <see cref="ServiceBusReceivedMessage"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public TestAzureServiceBusMessageProducer AddMessageBody<TMessageBody>(TMessageBody message)
        {
            return AddMessageBodies(new[] { message });
        }

        /// <summary>
        /// Add a series of custom <paramref name="messages"/> which will result in <see cref="ServiceBusReceivedMessage"/> on the message pump.
        /// </summary>
        /// <typeparam name="TMessageBody">The custom type of the message body.</typeparam>
        /// <param name="messages">The series of custom message bodies which will translate to <see cref="ServiceBusReceivedMessage"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public TestAzureServiceBusMessageProducer AddMessageBodies<TMessageBody>(IEnumerable<TMessageBody> messages)
        {
            Guard.NotNull(messages, nameof(messages), "Requires a series of message bodies to produce Azure Service Bus messages to the message pump");

            ServiceBusReceivedMessage CreateServiceBusMessage(TMessageBody message)
            {
                BinaryData body = null;
                if (message != null)
                {
                    body = BinaryData.FromObjectAsJson(message); 
                }

                return ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: body, 
                    messageId: Guid.NewGuid().ToString(),
                    correlationId: Guid.NewGuid().ToString(),
                    properties: new Dictionary<string, object>
                    {
                        [PropertyNames.Encoding] = Encoding.UTF8.WebName,
                        [PropertyNames.ContentType] = "application/json",
                        [PropertyNames.TransactionId] = Guid.NewGuid().ToString(),
                        [PropertyNames.OperationParentId] = Guid.NewGuid().ToString()
                    });
            }

            _createMessagesCollection.Add(() => messages.Select(CreateServiceBusMessage).ToArray());
            return this;
        }

        /// <summary>
        /// Produce an Azure Service Bus message like it would come from an actual Service Bus resource.
        /// </summary>
        public Task<ServiceBusReceivedMessage[]> ProduceMessagesAsync()
        {
            ServiceBusReceivedMessage[] messages = 
                _createMessagesCollection.SelectMany(createMessages => createMessages())
                                         .ToArray();

            return Task.FromResult(messages);
        }
    }
}
