using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Testing.Messaging.Pumps.EventHubs;

namespace Arcus.Testing.Tests.Unit.Messaging.EventHubs.Fixture
{
    public class SensorTelemetryAzureEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorTelemetry>
    {
        public bool IsProcessed { get; private set; }
        public Task ProcessMessageAsync(
            SensorTelemetry message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
