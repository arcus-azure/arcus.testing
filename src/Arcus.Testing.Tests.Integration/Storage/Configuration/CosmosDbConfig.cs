using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Azure.Core;
using Azure.ResourceManager.CosmosDB;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    public class CosmosDbConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbConfig" /> class.
        /// </summary>
        public CosmosDbConfig(ResourceIdentifier accountResourceId, string accountName, string databaseName)
        {
            ArgumentNullException.ThrowIfNull(accountResourceId);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

            AccountName = accountName;
            AccountEndpoint = new Uri($"https://{accountName}.documents.azure.com/");
            AccountResourceId = accountResourceId;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Gets the name of the Cosmos DB account associated with the current test setup.
        /// </summary>
        public string AccountName { get; }

        /// <summary>
        /// Gets the endpoint of the Cosmos DB account associated with the current test setup.
        /// </summary>
        public Uri AccountEndpoint { get; }

        /// <summary>
        /// Gets the resource ID of the Cosmos DB account associated with the current test setup.
        /// </summary>
        public ResourceIdentifier AccountResourceId { get; }

        /// <summary>
        /// Gets the name of the default database that was created on the Cosmos DB account associated with the current test setup.
        /// </summary>
        public string DatabaseName { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> for more test-friendly interaction.
    /// </summary>
    public static class CosmosDbTestConfigExtensions
    {
        /// <summary>
        /// Loads the <see cref="CosmosDbConfig"/> specific for the Mongo storage.
        /// </summary>
        public static CosmosDbConfig GetMongoDb(this TestConfig config)
        {
            return config.GetCosmosDb(CosmosDbType.MongoDb);
        }

        /// <summary>
        /// Loads the <see cref="CosmosDbConfig"/> specific for the NoSql storage.
        /// </summary>
        public static CosmosDbConfig GetNoSql(this TestConfig config)
        {
            return config.GetCosmosDb(CosmosDbType.NoSql);
        }

        private enum CosmosDbType { NoSql, MongoDb }

        private static CosmosDbConfig GetCosmosDb(this TestConfig config, CosmosDbType cosmosDbType)
        {
            AzureEnvironment env = config.GetAzureEnvironment();

            string cosmosDbName = config[$"Arcus:Cosmos:{cosmosDbType}:Name"];
            string databaseName = config[$"Arcus:Cosmos:{cosmosDbType}:DatabaseName"];
            ResourceIdentifier resourceId = CosmosDBAccountResource.CreateResourceIdentifier(env.SubscriptionId, env.ResourceGroupName, cosmosDbName);

            return new CosmosDbConfig(resourceId, cosmosDbName, databaseName);
        }
    }
}
