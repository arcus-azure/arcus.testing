using Azure.Core;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    public class CosmosDbConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbConfig" /> class.
        /// </summary>
        public CosmosDbConfig(ResourceIdentifier resourceId, MongoDbConfig mongoDb)
        {
            Name = resourceId.Name;
            ResourceId = resourceId;
            MongoDb = mongoDb;
        }

        public string Name { get; }
        public ResourceIdentifier ResourceId { get; }
        public MongoDbConfig MongoDb { get; }
    }

    public class MongoDbConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConfig" /> class.
        /// </summary>
        public MongoDbConfig(string databaseName)
        {
            DatabaseName = databaseName;
        }

        public string DatabaseName { get; }
    }

    public static class CosmosDbTestConfigExtensions
    {
        public static CosmosDbConfig GetCosmosDb(this TestConfig config)
        {
            string subscriptionId = config["Arcus:SubscriptionId"];
            string resourceGroupName = config["Arcus:ResourceGroup:Name"];
            string cosmosDbName = config["Arcus:CosmosDb:Name"];
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosDbName}");

            return new CosmosDbConfig(
                resourceId,
                new MongoDbConfig(config["Arcus:CosmosDb:MongoDb:DatabaseName"]));
        }
    }
}
