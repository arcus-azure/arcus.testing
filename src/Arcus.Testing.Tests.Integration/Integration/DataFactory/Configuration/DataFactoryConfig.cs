using Azure.Core;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    public class DataFactoryConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFactoryConfig" /> class.
        /// </summary>
        public DataFactoryConfig(
            string subscriptionId, 
            string resourceGroupName, 
            string resourceName)
        {
            Name = resourceName;
            ResourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DataFactory/factories/{resourceName}");
        }

        public string Name { get; }
        public ResourceIdentifier ResourceId { get; }
    }

    public static class TestConfigExtensions
    {
        public static DataFactoryConfig GetDataFactory(this TestConfig config)
        {
            return new DataFactoryConfig(
                config["Arcus:SubscriptionId"],
                config["Arcus:ResourceGroup:Name"],
                config["Arcus:DataFactory:Name"]);
        }
    }
}