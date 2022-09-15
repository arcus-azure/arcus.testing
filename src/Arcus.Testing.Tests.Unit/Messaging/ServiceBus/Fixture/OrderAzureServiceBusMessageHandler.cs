using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class OrderAzureServiceBusMessageHandler : AzureServiceBusMessageHandler<Order>
    {
        private readonly Order[] _expected;
        private int _expectedCount;

        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public OrderAzureServiceBusMessageHandler(params Order[] expected) : base(NullLogger.Instance)
        {
            _expected = expected;
            _expectedCount = 0;
        }

        public override async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Assert.Single(_expected, expected =>
            {
                return message != null
                       && message.OrderId == expected.OrderId
                       && message.Scheduled == expected.Scheduled
                       && message.Product?.ProductName == expected.Product.ProductName;
            });
            IsProcessed = ++_expectedCount == _expected.Length;
            await CompleteMessageAsync();
        }
    }
}
