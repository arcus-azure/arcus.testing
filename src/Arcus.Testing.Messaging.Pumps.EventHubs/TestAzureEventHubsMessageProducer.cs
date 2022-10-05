using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using GuardNet;

namespace Arcus.Testing.Messaging.Pumps.EventHubs
{
    /// <summary>
    /// Represents a message producer that produces Azure EventHubs messages like it would come from an actual Azure resource.
    /// </summary>
    public class TestAzureEventHubsMessageProducer : IAzureEventHubsMessageProducer
    {
        private readonly ICollection<Func<EventData[]>> _createMessagesCollection = new Collection<Func<EventData[]>>();

        /// <summary>
        /// Adds an Azure Service Bus <paramref name="message"/> on the message pump.
        /// </summary>
        /// <param name="message">The message to produce on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public TestAzureEventHubsMessageProducer AddMessage(EventData message)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to produce on the message pump");
            return AddMessages(new[] { message });
        }

        /// <summary>
        /// Adds a series of Azure Service Bus <paramref name="messages"/> on the message pump.
        /// </summary>
        /// <param name="messages">The series of messages to produce on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public TestAzureEventHubsMessageProducer AddMessages(IEnumerable<EventData> messages)
        {
            Guard.NotNull(messages, nameof(messages), "Requires a series of Azure Service Bus messages to produce on the message pump");

            _createMessagesCollection.Add(messages.ToArray);
            return this;
        }

        /// <summary>
        /// Add a series of custom <paramref name="message"/> which will result in <see cref="EventData"/> on the message pump.
        /// </summary>
        /// <typeparam name="TMessageBody">The custom type of the message body.</typeparam>
        /// <param name="message">The series of custom message bodies which will translate to <see cref="EventData"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public TestAzureEventHubsMessageProducer AddMessageBody<TMessageBody>(TMessageBody message)
        {
            return AddMessageBodies(new[] { message });
        }

        /// <summary>
        /// Add a series of custom <paramref name="messages"/> which will result in <see cref="EventData"/> on the message pump.
        /// </summary>
        /// <typeparam name="TMessageBody">The custom type of the message body.</typeparam>
        /// <param name="messages">The series of custom message bodies which will translate to <see cref="EventData"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public TestAzureEventHubsMessageProducer AddMessageBodies<TMessageBody>(IEnumerable<TMessageBody> messages)
        {
            Guard.NotNull(messages, nameof(messages), "Requires a series of message bodies to produce Azure Service Bus messages to the message pump");

            EventData CreateEventData(TMessageBody message)
            {
                EventData eventData =
                    EventDataBuilder.CreateForBody(message)
                                    .WithOperationId(Guid.NewGuid().ToString())
                                    .WithTransactionId(Guid.NewGuid().ToString())
                                    .WithOperationParentId(Guid.NewGuid().ToString())
                                    .Build();

                eventData.MessageId = Guid.NewGuid().ToString();
                return eventData;
            }

            _createMessagesCollection.Add(() => messages.Select(CreateEventData).ToArray());
            return this;
        }

        /// <summary>
        /// Produce an Azure EventHubs message like it would come from an actual EventHubs resource.
        /// </summary>
        public Task<EventData[]> ProduceMessagesAsync()
        {
            EventData[] messages = 
                _createMessagesCollection.SelectMany(createMessages => createMessages())
                                         .ToArray();

            return Task.FromResult(messages);
        }
    }
}
