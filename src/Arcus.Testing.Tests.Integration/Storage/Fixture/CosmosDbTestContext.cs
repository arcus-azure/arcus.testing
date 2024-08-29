using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Azure.Core;
using Azure.Identity;
using Bogus;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    public class CosmosDbTestContext : IAsyncDisposable
    {
        private readonly MongoClient _client;
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly IMongoDatabase _database;
        private readonly Collection<string> _collectionNames = new();
        private readonly TestConfig _config;
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbTestContext" /> class.
        /// </summary>
        private CosmosDbTestContext(
            TemporaryManagedIdentityConnection connection,
            IMongoDatabase database,
            TestConfig config,
            ILogger logger)
        {
            _connection = connection;
            _database = database;
            _config = config;
            _logger = logger;
        }

        public static async Task<CosmosDbTestContext> GivenMongoDbAsync(TestConfig config, ILogger logger)
        {
            var connection = TemporaryManagedIdentityConnection.Create(config.GetServicePrincipal());
            MongoClient mongoDbClient = await AuthenticateMongoDbClientAsync(config);
            IMongoDatabase database = mongoDbClient.GetDatabase(config["Arcus:CosmosDb:MongoDb:DatabaseName"]);

            return new CosmosDbTestContext(connection, database, config, logger);
        }

        private static async Task<MongoClient> AuthenticateMongoDbClientAsync(TestConfig config)
        {
            string scope = "https://management.azure.com/.default";
            var tokenProvider = new DefaultAzureCredential();
            AccessToken accessToken = await tokenProvider.GetTokenAsync(new TokenRequestContext(scopes: new[] { scope }));

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.Token}");

            string subscriptionId = config["Arcus:SubscriptionId"];
            string resourceGroupName = config["Arcus:ResourceGroup:Name"];
            string cosmosDbName = config["Arcus:CosmosDb:Name"];

            string listConnectionStringUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosDbName}/listConnectionStrings?api-version=2021-04-15";
            var response = await httpClient.PostAsync(listConnectionStringUrl, new StringContent(""));
            var responseBody = await response.Content.ReadAsStringAsync();
            var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(responseBody);
            string connectionString = connectionStrings["connectionStrings"][0]["connectionString"];

            return new MongoClient(connectionString);
        }

        public string WhenCollectionNameUnavailable()
        {
            string collectionName = $"test-{Bogus.Random.Guid()}";
            _collectionNames.Add(collectionName);

            return collectionName;
        }

        public async Task<string> WhenCollectionNameAvailableAsync()
        {
            string collectionName = WhenCollectionNameUnavailable();
            await _database.CreateCollectionAsync(collectionName);

            return collectionName;
        }

        public async Task<ObjectId> WhenDocumentAvailableAsync<T>(string collectionName, T document)
        {
            var bson = document.ToBsonDocument();
            var id = ObjectId.GenerateNewId();
            bson["_id"] = id;

            IMongoCollection<BsonDocument> collection = _database.GetCollection<BsonDocument>(collectionName);
            await collection.InsertOneAsync(bson);

            return id;
        }

        public async Task ShouldStoreCollectionAsync(string collectionName)
        {
            Assert.True(
                await StoresCollectionNameAsync(collectionName), 
                $"temporary mongo db collection '{collectionName}' should be available");
        }

        public async Task ShouldNotStoreCollectionAsync(string collectionName)
        {
            Assert.False(
                await StoresCollectionNameAsync(collectionName), 
                $"temporary mongo db collection '{collectionName}' should not be available");
        }

        private async Task<bool> StoresCollectionNameAsync(string collectionName)
        {
            var options = new ListCollectionNamesOptions
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName)
            };
            using IAsyncCursor<string> collectionNames = await _database.ListCollectionNamesAsync(options);
            return await collectionNames.AnyAsync();
        }

        public async Task ShouldStoreDocumentAsync<T>(string collectionName, ObjectId id, Action<T> assertion = null)
        {
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(elementName, id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Single(matchingDocs);

            assertion?.Invoke(matchingDocs[0]);
        }

        public async Task ShouldNotStoreDocumentAsync<T>(string collectionName, ObjectId id)
        {
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(elementName, id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Empty(matchingDocs);
        }

        private static FilterDefinition<T> CreateMatchingFilter<T>(T doc)
        {
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
            var bson = doc.ToBsonDocument();
            BsonValue id = bson[elementName];

            FilterDefinition<T> filter = Builders<T>.Filter.Eq(elementName, id);
            
            return filter;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            disposables.Add(_connection);

            foreach (string collectionName in _collectionNames)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await _database.DropCollectionAsync(collectionName);
                }));
            }
        }
    }
}
