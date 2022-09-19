using System;
using Bogus;
using Newtonsoft.Json;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class Shipment
    {
        [JsonProperty("container")]
        public Container Container { get; set; }

        [JsonProperty("arrived")]
        public DateTimeOffset Arrived { get; set; }

        public static Shipment Generate()
        {
            var containerGenerator = new Faker<Container>()
                .RuleFor(c => c.SerialNumber, faker => faker.Random.AlphaNumeric(12));

            var shipmentGenerator = new Faker<Shipment>()
                .RuleFor(s => s.Container, faker => containerGenerator.Generate())
                .RuleFor(s => s.Arrived, faker => faker.Date.RecentOffset());

            return shipmentGenerator.Generate();
        }
    }
}