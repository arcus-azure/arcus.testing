﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class OrderAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly Order[] _expected;
        private int _expectedCount;

        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public OrderAzureServiceBusMessageHandler(params Order[] expected)
        {
            _expected = expected;
            _expectedCount = 0;
        }

        public Task ProcessMessageAsync(
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
            
            return Task.CompletedTask;
        }
    }
}
