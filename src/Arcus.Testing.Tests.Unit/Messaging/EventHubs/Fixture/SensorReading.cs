using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;

namespace Arcus.Testing.Tests.Unit.Messaging.EventHubs.Fixture
{
    public class SensorReading
    {
        public string SensorId { get; set; }
        public double SensorValue { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public static SensorReading Generate()
        {
            var generator = new Faker<SensorReading>()
                .RuleFor(s => s.SensorId, f => f.Random.Guid().ToString())
                .RuleFor(s => s.SensorValue, f => f.Random.Double())
                .RuleFor(s => s.Timestamp, f => f.Date.RecentOffset());

            return generator.Generate();
        }
    }
}
