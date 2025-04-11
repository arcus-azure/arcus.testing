using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private static readonly HttpClient HttpClient = new();

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
            const string scope = "https://management.azure.com/.default";
            var tokenProvider = new DefaultAzureCredential();
            AccessToken accessToken = await tokenProvider.GetTokenAsync(new TokenRequestContext(scopes: new[] { scope }));

            AzureEnvironment env = config.GetAzureEnvironment();
            string subscriptionId = env.SubscriptionId;
            string resourceGroupName = env.ResourceGroupName;
            string cosmosDbName = config.GetMongoDb().AccountName;

            var listConnectionStringUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{cosmosDbName}/listConnectionStrings?api-version=2021-04-15";

            using var request = new HttpRequestMessage(HttpMethod.Post, listConnectionStringUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

            using HttpResponseMessage response = await HttpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            var connectionStringDic = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(responseBody);

            Assert.NotNull(connectionStringDic);
            List<Dictionary<string, string>> connectionStrings = connectionStringDic["connectionStrings"];
            Assert.NotEmpty(connectionStrings);

            Dictionary<string, string> primaryConnectionString = connectionStrings[0];
            Assert.NotEmpty(primaryConnectionString);

            string connectionString = primaryConnectionString["connectionString"];
            Assert.NotEmpty(connectionString);

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
        /// Provides a collection name that does exist in the MongoDb database.
        /// </summary>
        public async Task<string> WhenCollectionNameAvailableAsync()
        {
            string collectionName = WhenCollectionNameUnavailable();
            _logger.LogTrace("[Test] create MongoDb collection '{CollectionName}' outside the fixture's scope", collectionName);

            await WhenMongoDbAvailableAsync(
                () => _database.CreateCollectionAsync(collectionName),
                $"[Test] cannot create MongoDb collection '{collectionName}' outside the fixture's scope, due to a high-rate failure");

            return collectionName;
        }

        /// <summary>
        /// Runs an <paramref name="mongoDbOperationAsync"/> within a MongoDb connection, with transient-failure retry.
        /// </summary>
        /// <param name="mongoDbOperationAsync">The operation to run against a MongoDb database.</param>
        /// <param name="errorMessage">The custom user message that describes the <paramref name="mongoDbOperationAsync"/>.</param>
        internal static async Task WhenMongoDbAvailableAsync(Func<Task> mongoDbOperationAsync, string errorMessage)
        {
            await WhenMongoDbAvailableAsync(async () =>
            {
                await mongoDbOperationAsync();
                return 0;

            }, errorMessage);
        }

        /// <summary>
        /// Runs an <paramref name="mongoDbOperationAsync"/> within a MongoDb connection, with transient-failure retry.
        /// </summary>
        /// <param name="mongoDbOperationAsync">The operation to run against a MongoDb database.</param>
        /// <param name="errorMessage">The custom user message that describes the <paramref name="mongoDbOperationAsync"/>.</param>
        internal static async Task<TResult> WhenMongoDbAvailableAsync<TResult>(Func<Task<TResult>> mongoDbOperationAsync, string errorMessage)
        {
            return await Poll.Target<TResult, MongoCommandException>(mongoDbOperationAsync)
                             .When(ex => ex.Message.Contains("high rate") || ex.ErrorMessage.Contains("high rate"))
                             .Every(TimeSpan.FromMilliseconds(500))
                             .FailWith(errorMessage);
        }

        /// <summary>
        /// Deletes an existing container in the NoSql database.
        /// </summary>
        public async Task WhenCollectionDeletedAsync(string collectionName)
        {
            _logger.LogTrace("[Test] delete MongoDb collection '{CollectionName}' outside test fixture's scope", collectionName);
            await _database.DropCollectionAsync(collectionName);
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

            _logger.LogTrace("[Test] add '{DocType}' item '{DocId}' to MongoDb collection '{CollectionName}'", typeof(T).Name, id, collectionName);
            await collection.InsertOneAsync(bson);

            return id;
        }

        /// <summary>
        /// Deletes an existing document in the MongoDb collection.
        /// </summary>
        public async Task WhenDocumentDeletedAsync<T>(string collectionName, BsonValue id)
        {
            _logger.LogTrace("[Test] delete MongoDb document '{DocId}' in collection '{CollectionName}' outside test fixture's scope", id, collectionName);

            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            FilterDefinition<T> filter = CreateIdFilter<T>(id);

            await collection.DeleteOneAsync(filter);
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
            FilterDefinition<T> filter = CreateIdFilter<T>(id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Single(matchingDocs);

            assertion?.Invoke(matchingDocs[0]);
        }

        /// <summary>
        /// Verifies that a document does not exist in the MongoDb collection.
        /// </summary>
        public async Task ShouldNotStoreDocumentAsync<T>(string collectionName, BsonValue id)
        {
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            FilterDefinition<T> filter = CreateIdFilter<T>(id);

            List<T> matchingDocs = await collection.Find(filter).ToListAsync();
            Assert.Empty(matchingDocs);
        }

        private static FilterDefinition<T> CreateIdFilter<T>(BsonValue id)
        {
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(T));
            string elementName = classMap.IdMemberMap.ElementName;
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
