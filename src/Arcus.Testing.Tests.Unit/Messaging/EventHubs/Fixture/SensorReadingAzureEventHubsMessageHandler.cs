using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Messaging.EventHubs.Fixture
{
    public class SensorReadingAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
    {
        private readonly SensorReading[] _expectedReadings;
        private int _expectedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingAzureEventHubsMessageHandler" /> class.
        /// </summary>
        public SensorReadingAzureEventHubsMessageHandler(params SensorReading[] expectedReadings)
        {
            _expectedReadings = expectedReadings;
        }

        public bool IsProcessed { get; private set; }
        
        public Task ProcessMessageAsync(
            SensorReading message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Assert.Single(_expectedReadings, expected =>
            {
                return expected.SensorId == message.SensorId
                       && Math.Abs(expected.SensorValue - message.SensorValue) == 0
                       && expected.Timestamp == message.Timestamp;
            });

            IsProcessed = ++_expectedCount == _expectedReadings.Length;
            return Task.CompletedTask;
        }
    }
}
