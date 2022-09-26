using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.Testing.Tests.Unit.Messaging.EventHubs.Fixture
{
    public class SensorTelemetry
    {
        public string SensorId { get; set; }
        public HealthStatus SensorStatus { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public static SensorTelemetry Generate()
        {
            var generator = new Faker<SensorTelemetry>()
                .RuleFor(s => s.SensorId, f => f.Random.Guid().ToString())
                .RuleFor(s => s.SensorStatus, f => f.PickRandom<HealthStatus>())
                .RuleFor(s => s.Timestamp, f => f.Date.RecentOffset());

            return generator.Generate();
        }
    }
}
