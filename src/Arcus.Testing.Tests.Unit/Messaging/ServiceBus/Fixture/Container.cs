using Newtonsoft.Json;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class Container
    {
        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }
    }
}