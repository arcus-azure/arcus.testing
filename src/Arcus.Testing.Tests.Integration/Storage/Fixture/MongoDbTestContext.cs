using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
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

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    /// <summary>
    /// Provides test-friendly interactions with Azure MongoDb resources.
    /// </summary>
    public class MongoDbTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly IMongoDatabase _database;
        private readonly Collection<string> _collectionNames = new();
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTestContext" /> class.
        /// </summary>
        private MongoDbTestContext(
            TemporaryManagedIdentityConnection connection,
            IMongoDatabase database,
            ILogger logger)
        {
            _connection = connection;
            _database = database;
            _logger = logger;
        }

        /// <summary>
        /// Creates an authenticated <see cref="MongoDbTestContext"/>.
        /// </summary>
        public static async Task<MongoDbTestContext> GivenAsync(TestConfig config, ILogger logger)
        {
            var connection = TemporaryManagedIdentityConnection.Create(config.GetServicePrincipal());
            MongoClient mongoDbClient = await AuthenticateMongoDbClientAsync(config);
            IMongoDatabase database = mongoDbClient.GetDatabase(config["Arcus:Cosmos:MongoDb:DatabaseName"]);

            return new MongoDbTestContext(connection, database, logger);
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
            string cosmosDbName = config["Arcus:Cosmos:MongoDb:Name"];

            string listConnectionStringUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosDbName}/listConnectionStrings?api-version=2021-04-15";
            var response = await httpClient.PostAsync(listConnectionStringUrl, new StringContent(""));
            var responseBody = await response.Content.ReadAsStringAsync();
            var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(responseBody);
            string connectionString = connectionStrings["connectionStrings"][0]["connectionString"];

            return new MongoClient(connectionString);
        }

        /// <summary>
        /// Provides a collection name that does not exists in the MongoDb database.
        /// </summary>
        public string WhenCollectionNameUnavailable()
        {
            string collectionName = $"test-{Bogus.Random.Guid()}";
            _collectionNames.Add(collectionName);

            return collectionName;
        }

        /// <summary>
        /// Provides a collection name that does exists in the MongoDb database.
        /// </summary>
        public async Task<string> WhenCollectionNameAvailableAsync()
        {
            string collectionName = WhenCollectionNameUnavailable();
            await _database.CreateCollectionAsync(collectionName);

            return collectionName;
        }

        /// <summary>
        /// Provides an existing document in a MongoDb collection.
        /// </summary>
        public async Task<BsonValue> WhenDocumentAvailableAsync<T>(string collectionName, T document)
        {
            IMongoCollection<BsonDocument> collection = _database.GetCollection<BsonDocument>(collectionName);
            
            var bson = document.ToBsonDocument();
            
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;

            object newId = classMap.IdMemberMap.IdGenerator.GenerateId(collection, document);
            BsonValue id = BsonTypeMapper.MapToBsonValue(newId);
            bson[elementName] = id;

            await collection.InsertOneAsync(bson);
            return id;
        }

        /// <summary>
        /// Verifies that a collection exists in the MongoDb database.
        /// </summary>
        public async Task ShouldStoreCollectionAsync(string collectionName)
        {
            Assert.True(
                await StoresCollectionNameAsync(collectionName), 
                $"temporary mongo db collection '{collectionName}' should be available");
        }

        /// <summary>
        /// Verifies that a collection exists in the MongoDb database.
        /// </summary>
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

        /// <summary>
        /// Verifies that a document exists in the MongoDb collection.
        /// </summary>
        public async Task ShouldStoreDocumentAsync<T>(string collectionName, BsonValue id, Action<T> assertion = null)
        {
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(elementName, id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Single(matchingDocs);

            assertion?.Invoke(matchingDocs[0]);
        }

        /// <summary>
        /// Verifies that a document does not exists in the MongoDb collection.
        /// </summary>
        public async Task ShouldNotStoreDocumentAsync<T>(string collectionName, BsonValue id)
        {
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
            FilterDefinition<T> filter = Builders<T>.Filter.Eq(elementName, id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Empty(matchingDocs);
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
