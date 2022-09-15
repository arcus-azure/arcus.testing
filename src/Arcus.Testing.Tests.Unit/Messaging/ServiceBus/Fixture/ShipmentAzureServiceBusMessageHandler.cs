using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class ShipmentAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
    {
        private readonly Shipment[] _expected;
        private int _expectedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public ShipmentAzureServiceBusMessageHandler()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public ShipmentAzureServiceBusMessageHandler(params Shipment[] expected)
        {
            _expected = expected;
        }

        public bool IsProcessed { get; private set; }

        public Task ProcessMessageAsync(
            Shipment message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Assert.Single(_expected, expected =>
            {
                return message != null
                       && message.Arrived == expected.Arrived
                       && message.Container?.SerialNumber == expected.Container.SerialNumber;
            });
            IsProcessed = ++_expectedCount == _expected.Length;

            return Task.CompletedTask;
        }
    }
}