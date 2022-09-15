using Newtonsoft.Json;

namespace Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture
{
    public class Product
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }
    }
}