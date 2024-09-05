using System;
using Azure.Core;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    public class CosmosDbConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbConfig" /> class.
        /// </summary>
        public CosmosDbConfig(ResourceIdentifier resourceId, MongoDbConfig mongoDb, NoSqlConfig noSql)
        {
            Name = resourceId.Name;
            Endpoint = new Uri("https://arcus-testing-cosmos.mongo.cosmos.azure.com/").ToString();
            ResourceId = resourceId;
            MongoDb = mongoDb;
            NoSql = noSql;
        }

        public string Name { get; }
        public string Endpoint { get; }
        public ResourceIdentifier ResourceId { get; }
        public MongoDbConfig MongoDb { get; }
        public NoSqlConfig NoSql { get; }
    }

    public class MongoDbConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConfig" /> class.
        /// </summary>
        public MongoDbConfig(ResourceIdentifier resourceId, string accountName, string databaseName)
        {
            ResourceId = resourceId;
            Name = accountName;
            DatabaseName = databaseName;
        }

        public string Name { get; }
        public ResourceIdentifier ResourceId { get; }
        public string DatabaseName { get; }
    }

    public class NoSqlConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoSqlConfig" /> class.
        /// </summary>
        public NoSqlConfig(ResourceIdentifier resourceId, string accountName, string databaseName)
        {
            ResourceId = resourceId;
            DatabaseName = databaseName;
            Endpoint = new Uri($"https://{accountName}.documents.azure.com/").ToString();
        }

        public string Endpoint { get; }
        public ResourceIdentifier ResourceId { get; }
        public string DatabaseName { get; }
    }

    public static class CosmosDbTestConfigExtensions
    {
        public static MongoDbConfig GetMongoDb(this TestConfig config)
        {
            string subscriptionId = config["Arcus:SubscriptionId"];
            string resourceGroupName = config["Arcus:ResourceGroup:Name"];
            string cosmosDbName = config["Arcus:Cosmos:MongoDb:Name"];
            var resourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosDbName}");
            string databaseName = config["Arcus:Cosmos:MongoDb:DatabaseName"];

            return new MongoDbConfig(
                resourceId,
                cosmosDbName,
                databaseName);
        }

        public static NoSqlConfig GetNoSql(this TestConfig config)
        {
            ResourceIdentifier resourceId = CreateResourceId(config, config["Arcus:Cosmos:NoSql:Name"]);

            return new NoSqlConfig(
                resourceId,
                config["Arcus:Cosmos:NoSql:Name"],
                config["Arcus:Cosmos:NoSql:DatabaseName"]);
        }

        private static ResourceIdentifier CreateResourceId(TestConfig config, string accountName)
        {
            string subscriptionId = config["Arcus:SubscriptionId"];
            string resourceGroupName = config["Arcus:ResourceGroup:Name"];
            
            return ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}");
        }
    }
}
