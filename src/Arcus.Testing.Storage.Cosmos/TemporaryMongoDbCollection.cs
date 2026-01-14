using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure.Core;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryMongoDbCollection"/> is created.
    /// </summary>
    internal enum OnSetupMongoDbCollection { LeaveExisted = 0, CleanIfExisted, CleanIfMatched }

    /// <summary>
    /// Represents the available options when the <see cref="TemporaryMongoDbCollection"/> is deleted.
    /// </summary>
    internal enum OnTeardownMongoDbCollection { CleanIfUpserted = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryMongoDbCollection"/>.
    /// </summary>
    public class OnSetupMongoDbCollectionOptions
    {
        /// <summary>
        /// Gets the configurable setup option on what to do with existing MongoDB documents in the Azure Cosmos DB for MongoDB collection upon the test fixture creation.
        /// </summary>
        internal OnSetupMongoDbCollection Documents { get; private set; }

        /// <summary>
        /// Gets the filter expression that determines if MongoDB documents should be cleaned
        /// when the <see cref="Documents"/> is configured as <see cref="OnSetupMongoDbCollection.CleanIfMatched"/>.
        /// </summary>
        internal FilterDefinition<BsonDocument> IsMatched { get; private set; } = Builders<BsonDocument>.Filter.Where(_ => false);

        /// <summary>
        /// (default) Configures the <see cref="TemporaryMongoDbCollection"/> to leave all MongoDB documents untouched
        /// that already existed upon the test fixture creation, when there was already an Azure Cosmos DB for MongoDB collection available.
        /// </summary>
        public OnSetupMongoDbCollectionOptions LeaveAllDocuments()
        {
            Documents = OnSetupMongoDbCollection.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete all the already existing MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon the test fixture creation.
        /// </summary>
        public OnSetupMongoDbCollectionOptions CleanAllDocuments()
        {
            Documents = OnSetupMongoDbCollection.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon the test fixture creation that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will aggregate together in an OR expression.
        /// </remarks>
        /// <typeparam name="T">The type of the documents in the Azure Cosmos DB for MongoDB collection.</typeparam>
        /// <param name="filter">The filter expression to match MongoDB documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnSetupMongoDbCollectionOptions CleanMatchingDocuments<T>(Expression<Func<T, bool>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return CleanMatchingDocuments((FilterDefinition<T>) filter);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon the test fixture creation that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will aggregate together in an OR expression.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDb collection.</typeparam>
        /// <param name="filter">The filter expression to match MongoDB documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnSetupMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(FilterDefinition<TDocument> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            IBsonSerializerRegistry serializerRegistry = BsonSerializer.SerializerRegistry;
            IBsonSerializer<TDocument> documentSerializer = serializerRegistry.GetSerializer<TDocument>();
            BsonDocument doc = filter.Render(new RenderArgs<TDocument>(documentSerializer, serializerRegistry));

            IsMatched = Builders<BsonDocument>.Filter.Or(IsMatched, doc);
            Documents = OnSetupMongoDbCollection.CleanIfMatched;

            return this;
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryMongoDbCollection"/>.
    /// </summary>
    public class OnTeardownMongoDbCollectionOptions
    {
        /// <summary>
        /// Gets the configurable setup option on what to do with existing MongoDB documents in the Azure Cosmos DB for MongoDB collection upon the test fixture deletion.
        /// </summary>
        internal OnTeardownMongoDbCollection Documents { get; private set; }

        /// <summary>
        /// Gets the filter expression that determines if MongoDB documents should be cleaned
        /// when the <see cref="Documents"/> is configured as <see cref="OnTeardownMongoDbCollection.CleanIfMatched"/>.
        /// </summary>
        internal FilterDefinition<BsonDocument> IsMatched { get; private set; } = Builders<BsonDocument>.Filter.Where(_ => false);

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryMongoDbCollection"/> to only delete the MongoBb documents
        /// in an Azure Cosmos DB for MongoDB collection upon disposal if the document was inserted by the test fixture
        /// (using <see cref="TemporaryMongoDbCollection.UpsertDocumentAsync{TDocument}"/>).
        /// </summary>
        [Obsolete("Will be removed in v3, please use the " + nameof(CleanUpsertedDocuments) + "instead that provides exactly the same functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public OnTeardownMongoDbCollectionOptions CleanCreatedDocuments()
        {
            return CleanUpsertedDocuments();
        }

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryMongoDbCollection"/> to only delete or revert the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon disposal if the document was upserted by the test fixture
        /// (using <see cref="TemporaryMongoDbCollection.UpsertDocumentAsync{TDocument}"/>).
        /// </summary>
        public OnTeardownMongoDbCollectionOptions CleanUpsertedDocuments()
        {
            Documents = OnTeardownMongoDbCollection.CleanIfUpserted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete all the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon disposal - even if the test fixture didn't insert them.
        /// </summary>
        public OnTeardownMongoDbCollectionOptions CleanAllDocuments()
        {
            Documents = OnTeardownMongoDbCollection.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon disposal that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///   <para>Multiple calls will aggregate together in an OR expression.</para>
        ///   <para>
        ///     The matching of documents only happens on MongoDB documents that were created outside the scope of the test fixture.
        ///     All documents upserted by the test fixture will be deleted or reverted upon disposal, even if the documents do not match one of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        ///   </para>
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDB collection.</typeparam>
        /// <param name="filter">The filter expression to match MongoDB documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnTeardownMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(Expression<Func<TDocument, bool>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return CleanMatchingDocuments((FilterDefinition<TDocument>) filter);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDB documents
        /// in an Azure Cosmos DB for MongoDB collection upon disposal that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///   <para>Multiple calls will aggregate together in an OR expression.</para>
        ///   <para>
        ///     The matching of documents only happens on MongoDB documents that were created outside the scope of the test fixture.
        ///     All documents upserted by the test fixture will be deleted or reverted upon disposal, even if the documents do not match one of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        ///   </para>
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDB collection.</typeparam>
        /// <param name="filter">The filter expression to match MongoDB documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnTeardownMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(FilterDefinition<TDocument> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            IBsonSerializerRegistry serializerRegistry = BsonSerializer.SerializerRegistry;
            IBsonSerializer<TDocument> documentSerializer = serializerRegistry.GetSerializer<TDocument>();
            BsonDocument doc = filter.Render(new RenderArgs<TDocument>(documentSerializer, serializerRegistry));

            IsMatched = Builders<BsonDocument>.Filter.Or(IsMatched, doc);
            Documents = OnTeardownMongoDbCollection.CleanIfMatched;

            return this;
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryMongoDbCollection"/>.
    /// </summary>
    public class TemporaryMongoDbCollectionOptions
    {
        /// <summary>
        /// Gets the additional options to manipulate the creation of the <see cref="TemporaryMongoDbCollection"/>.
        /// </summary>
        public OnSetupMongoDbCollectionOptions OnSetup { get; } = new OnSetupMongoDbCollectionOptions().LeaveAllDocuments();

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryMongoDbCollection"/>.
        /// </summary>
        public OnTeardownMongoDbCollectionOptions OnTeardown { get; } = new OnTeardownMongoDbCollectionOptions().CleanUpsertedDocuments();
    }

    /// <summary>
    /// Represents a temporary Azure Cosmos DB for MongoDB collection that will be deleted after the instance is disposed.
    /// </summary>
#pragma warning disable CA1711 // Type ends with '...Collection' because it represents a 'MongoDB collection' in Azure Cosmos DB, not because it is a .NET type.
    public class TemporaryMongoDbCollection : IAsyncDisposable
#pragma warning restore CA1711
    {
        private readonly bool _createdByUs, _clientCreatedByUs;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly TemporaryMongoDbCollectionOptions _options;
        private readonly Collection<IAsyncDisposable> _documents = [];
        private readonly DisposableCollection _disposables;
        private readonly ILogger _logger;

        private TemporaryMongoDbCollection(
            bool createdByUs,
            string collectionName,
            MongoClient client,
            bool clientCreatedByUs,
            IMongoDatabase database,
            ILogger logger,
            TemporaryMongoDbCollectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(collectionName);
            ArgumentNullException.ThrowIfNull(options);

            _createdByUs = createdByUs;
            _client = client;
            _clientCreatedByUs = clientCreatedByUs;
            _database = database;
            _options = options;
            _logger = logger ?? NullLogger.Instance;
            _disposables = new DisposableCollection(_logger);

            Name = collectionName;
        }

        /// <summary>
        /// Gets the unique name of the MongoDB collection, currently available on Azure Cosmos DB.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryMongoDbCollection"/>.
        /// </summary>
        public OnTeardownMongoDbCollectionOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos DB for MongoDB collection if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbResourceId">
        ///   <para>The resource ID pointing towards the Azure Cosmos DB account.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="databaseName">The name of the MongoDB database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or <paramref name="collectionName"/> is blank.</exception>
        public static Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            ILogger logger)
        {
            return CreateIfNotExistsAsync(cosmosDbResourceId, databaseName, collectionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos DB for MongoDB collection if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbResourceId">
        ///   <para>The resource ID pointing towards the Azure Cosmos DB account.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="databaseName">The name of the MongoDB database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB collection.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            ILogger logger,
            Action<TemporaryMongoDbCollectionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(cosmosDbResourceId);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
            logger ??= NullLogger.Instance;

            MongoClient client = await MongoDbConnection.AuthenticateMongoClientAsync(cosmosDbResourceId, databaseName, collectionName, logger).ConfigureAwait(false);
            IMongoDatabase database = client.GetDatabase(databaseName);

            return await CreateIfNotExistsAsync(client, clientCreatedByUs: true, database, collectionName, logger, configureOptions).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos DB for MongoDB collection if it doesn't exist yet.
        /// </summary>
        /// <param name="database">The client to the MongoDB database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="database"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="collectionName"/> is blank.</exception>
        public static Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(IMongoDatabase database, string collectionName, ILogger logger)
        {
            return CreateIfNotExistsAsync(database, collectionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos DB for MongoDB collection if it doesn't exist yet.
        /// </summary>
        /// <param name="database">The client to the MongoDB database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB collection.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="database"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="collectionName"/> is blank.</exception>
        public static Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            IMongoDatabase database,
            string collectionName,
            ILogger logger,
            Action<TemporaryMongoDbCollectionOptions> configureOptions)
        {
            return CreateIfNotExistsAsync(client: null, clientCreatedByUs: false, database, collectionName, logger, configureOptions);
        }

        private static async Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            MongoClient client,
            bool clientCreatedByUs,
            IMongoDatabase database,
            string collectionName,
            ILogger logger,
            Action<TemporaryMongoDbCollectionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
            logger ??= NullLogger.Instance;

            var options = new TemporaryMongoDbCollectionOptions();
            configureOptions?.Invoke(options);

            var listOptions = new ListCollectionNamesOptions
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName)
            };
            using IAsyncCursor<string> collectionNames = await database.ListCollectionNamesAsync(listOptions).ConfigureAwait(false);
            if (await collectionNames.AnyAsync().ConfigureAwait(false))
            {
                logger.LogSetupUseExistingCollection(collectionName, database.DatabaseNamespace.DatabaseName);

                await CleanCollectionOnSetupAsync(database, collectionName, options, logger).ConfigureAwait(false);
                return new TemporaryMongoDbCollection(createdByUs: false, collectionName, client, clientCreatedByUs, database, logger, options);
            }

            logger.LogSetupCreateNewCollection(collectionName, database.DatabaseNamespace.DatabaseName);
            await database.CreateCollectionAsync(collectionName).ConfigureAwait(false);

            return new TemporaryMongoDbCollection(createdByUs: true, collectionName, client, clientCreatedByUs, database, logger, options);
        }

        private static Task CleanCollectionOnSetupAsync(IMongoDatabase database, string collectionName, TemporaryMongoDbCollectionOptions options, ILogger logger)
        {
            if (options.OnSetup.Documents is OnSetupMongoDbCollection.LeaveExisted)
            {
                return Task.CompletedTask;
            }

            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

            if (options.OnSetup.Documents is OnSetupMongoDbCollection.CleanIfExisted)
            {
                logger.LogSetupCleanAllDocuments(database.DatabaseNamespace.DatabaseName, collectionName);
                return collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
            }

            if (options.OnSetup.Documents is OnSetupMongoDbCollection.CleanIfMatched)
            {
                logger.LogSetupCleanAllMatchingDocuments(database.DatabaseNamespace.DatabaseName, collectionName);
                return collection.DeleteManyAsync(options.OnSetup.IsMatched);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the client to interact with the collection that is temporary available.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document in the MongoDb collection.</typeparam>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public IMongoCollection<TDocument> GetCollectionClient<TDocument>()
        {
            ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
            return _database.GetCollection<TDocument>(Name);
        }

        /// <summary>
        /// Inserts a temporary <paramref name="document"/> to the Azure Cosmos DB for MongoDB collection.
        /// </summary>
        /// <remarks>
        ///     All MongoDB documents inserted via the temporary collection will always be removed when the collection disposes,
        ///     regardless of what teardown options were configured on the collection.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the document in the MongoDB collection.</typeparam>
        /// <param name="document">The document to upload to the MongoDB collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="document"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3, please use the " + nameof(UpsertDocumentAsync) + "instead that provides exactly the same functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public Task AddDocumentAsync<TDocument>(TDocument document)
        {
            return UpsertDocumentAsync(document);
        }

        /// <summary>
        /// Adds a new or replaces an existing document in the Azure Cosmos DB for MongoDB collection (a.k.a. UPSERT).
        /// </summary>
        /// <remarks>
        ///     ⚡ Any items upserted via this call will always be deleted (if new) or reverted (if existing)
        ///     when the <see cref="TemporaryMongoDbCollection"/> is disposed.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the document in the MongoDB collection.</typeparam>
        /// <param name="document">The document to upload to the MongoDB collection.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="document"/> is <c>null</c>.</exception>
        public async Task UpsertDocumentAsync<TDocument>(TDocument document)
        {
            ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
            ArgumentNullException.ThrowIfNull(document);

            IMongoCollection<TDocument> collection = GetCollectionClient<TDocument>();
            _documents.Add(await TemporaryMongoDbDocument.UpsertDocumentAsync(collection, document, _logger).ConfigureAwait(false));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposables.IsDisposed)
            {
                return;
            }

            await using (_disposables.ConfigureAwait(false))
            {
                _disposables.AddRange(_documents);

                if (_createdByUs)
                {
                    _disposables.Add(AsyncDisposable.Create(() =>
                    {
                        _logger.LogTeardownDeleteCollection(Name, _database.DatabaseNamespace.DatabaseName);
                        return _database.DropCollectionAsync(Name);
                    }));
                }
                else
                {
                    _disposables.Add(AsyncDisposable.Create(CleanCollectionOnTeardownAsync));
                }

                if (_clientCreatedByUs && _client != null)
                {
                    _disposables.Add(_client);
                }

                GC.SuppressFinalize(this);
            }
        }

        private Task CleanCollectionOnTeardownAsync()
        {
            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanIfUpserted)
            {
                return Task.CompletedTask;
            }

            IMongoCollection<BsonDocument> collection = _database.GetCollection<BsonDocument>(Name);

            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanAll)
            {
                _logger.LogTeardownCleanAllDocuments(_database.DatabaseNamespace.DatabaseName, Name);
                return collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
            }

            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanIfMatched)
            {
                _logger.LogTeardownCleanAllMatchingDocuments(_database.DatabaseNamespace.DatabaseName, Name);
                return collection.DeleteManyAsync(_options.OnTeardown.IsMatched);
            }

            return Task.CompletedTask;
        }
    }

    internal static partial class TempMongoDbCollectionILoggerExtensions
    {
        internal const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Create new Azure Cosmos DB MongoDB collection '{CollectionName}' in database '{DatabaseName}'")]
        internal static partial void LogSetupCreateNewCollection(this ILogger logger, string collectionName, string databaseName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Use already existing Azure Cosmos DB for MongoDB collection '{CollectionName}' in database '{DatabaseName}'")]
        internal static partial void LogSetupUseExistingCollection(this ILogger logger, string collectionName, string databaseName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Clean all documents in Azure Cosmos DB MongoDB collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogSetupCleanAllDocuments(this ILogger logger, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Clean all matching documents in Azure Cosmos DB MongoDB collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogSetupCleanAllMatchingDocuments(this ILogger logger, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Clean all documents in Azure Cosmos DB for MongoDB collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogTeardownCleanAllDocuments(this ILogger logger, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Clean all matching documents in Azure Cosmos DB for MongoDB collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogTeardownCleanAllMatchingDocuments(this ILogger logger, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Cosmos DB for MongoDB collection '{CollectionName}' in database '{DatabaseName}'")]
        internal static partial void LogTeardownDeleteCollection(this ILogger logger, string collectionName, string databaseName);
    }
}
