using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure.Core;
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
    internal enum OnTeardownMongoDbCollection { CleanIfCreated = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryMongoDbCollection"/>.
    /// </summary>
    public class OnSetupMongoDbCollectionOptions
    {
        /// <summary>
        /// Gets the configurable setup option on what to do with existing MongoDb documents in the Azure MongoDb collection upon the test fixture creation.
        /// </summary>
        internal OnSetupMongoDbCollection Documents { get; private set; }

        /// <summary>
        /// Gets the filter expression that determines if MongoDb documents should be cleaned when the <see cref="Documents"/> is configured as <see cref="OnSetupMongoDbCollection.CleanIfMatched"/>.
        /// </summary>
        internal FilterDefinition<BsonDocument> IsMatched { get; private set; } = Builders<BsonDocument>.Filter.Where(_ => false);

        /// <summary>
        /// (default) Configures the <see cref="TemporaryMongoDbCollection"/> to leave all MongoDb documents untouched
        /// that already existed upon the test fixture creation, when there was already an Azure MongoDb collection available.
        /// </summary>
        public OnSetupMongoDbCollectionOptions LeaveAllDocuments()
        {
            Documents = OnSetupMongoDbCollection.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete all the already existing MongoDb documents upon the test fixture creation.
        /// </summary>
        public OnSetupMongoDbCollectionOptions CleanAllDocuments()
        {
            Documents = OnSetupMongoDbCollection.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDb documents upon the test fixture creation that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will aggregated together in an OR expression.
        /// </remarks>
        /// <typeparam name="T">The type of the documents in the MongoDb collection.</typeparam>
        /// <param name="filter">The filter expression to match documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnSetupMongoDbCollectionOptions CleanMatchingDocuments<T>(Expression<Func<T, bool>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return CleanMatchingDocuments((FilterDefinition<T>) filter);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDb documents upon the test fixture creation that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will aggregated together in an OR expression.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDb collection.</typeparam>
        /// <param name="filter">The filter expression to match documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnSetupMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(FilterDefinition<TDocument> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            IBsonSerializerRegistry serializerRegistry = BsonSerializer.SerializerRegistry;
            IBsonSerializer<TDocument> documentSerializer = serializerRegistry.GetSerializer<TDocument>();
            BsonDocument doc = filter.Render(documentSerializer, serializerRegistry);

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
        /// Gets the configurable setup option on what to do with existing MongoDb documents in the Azure MongoDb collection upon the test fixture deletion.
        /// </summary>
        internal OnTeardownMongoDbCollection Documents { get; private set; }

        /// <summary>
        /// Gets the filter expression that determines if MongoDb documents should be cleaned when the <see cref="Documents"/> is configured as <see cref="OnTeardownMongoDbCollection.CleanIfMatched"/>.
        /// </summary>
        internal FilterDefinition<BsonDocument> IsMatched { get; private set; } = Builders<BsonDocument>.Filter.Where(_ => false);

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryMongoDbCollection"/> to only delete the MongoDb documents upon disposal
        /// if the document was inserted by the test fixture (using <see cref="TemporaryMongoDbCollection.InsertDocumentAsync{TDocument}(TDocument)"/>).
        /// </summary>
        public OnTeardownMongoDbCollectionOptions CleanCreatedDocuments()
        {
            Documents = OnTeardownMongoDbCollection.CleanIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete all the MongoDb documents upon disposal - even if the test fixture didn't inserted them.
        /// </summary>
        public OnTeardownMongoDbCollectionOptions CleanAllDocuments()
        {
            Documents = OnTeardownMongoDbCollection.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDb documents upon disposal that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     The matching of documents only happens on MongoDb documents that were created outside the scope of the test fixture.
        ///     All documents created by the test fixture will be deleted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDb collection.</typeparam>
        /// <param name="filter">The filter expression to match documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnTeardownMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(Expression<Func<TDocument, bool>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return CleanMatchingDocuments((FilterDefinition<TDocument>) filter);
        }

        /// <summary>
        /// Configures the <see cref="TemporaryMongoDbCollection"/> to delete the MongoDb documents upon disposal that matched the configured <paramref name="filter"/>.
        /// </summary>
        /// <remarks>
        ///     The matching of documents only happens on MongoDb documents that were created outside the scope of the test fixture.
        ///     All documents created by the test fixture will be deleted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the documents in the MongoDb collection.</typeparam>
        /// <param name="filter">The filter expression to match documents that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public OnTeardownMongoDbCollectionOptions CleanMatchingDocuments<TDocument>(FilterDefinition<TDocument> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            IBsonSerializerRegistry serializerRegistry = BsonSerializer.SerializerRegistry;
            IBsonSerializer<TDocument> documentSerializer = serializerRegistry.GetSerializer<TDocument>();
            BsonDocument doc = filter.Render(documentSerializer, serializerRegistry);

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
        public OnTeardownMongoDbCollectionOptions OnTeardown { get; } = new OnTeardownMongoDbCollectionOptions().CleanCreatedDocuments();
    }

    /// <summary>
    /// Represents a temporary Azure Cosmos MongoDb collection that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryMongoDbCollection : IAsyncDisposable
    {
        private readonly bool _createdByUs;
        private readonly string _collectionName;
        private readonly IMongoDatabase _database;
        private readonly TemporaryMongoDbCollectionOptions _options;
        private readonly Collection<IAsyncDisposable> _documents = new();
        private readonly ILogger _logger;

        private TemporaryMongoDbCollection(
            bool createdByUs,
            string collectionName,
            IMongoDatabase database,
            ILogger logger,
            TemporaryMongoDbCollectionOptions options)
        {
            _createdByUs = createdByUs;
            _collectionName = collectionName;
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _options = options ?? new TemporaryMongoDbCollectionOptions();
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos MongoDb collection if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbResourceId">The resource ID pointing towards the Azure Cosmos account.</param>
        /// <param name="databaseName">The name of the MongoDb database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(cosmosDbResourceId, databaseName, collectionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos MongoDb collection if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbResourceId">The resource ID pointing towards the Azure Cosmos account.</param>
        /// <param name="databaseName">The name of the MongoDb database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb collection.</param>
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
            logger ??= NullLogger.Instance;
            
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Cosmos MongoDb database name to create a temporary collection");
            }

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Cosmos MongoDb collection name to create a temporary collection");
            }

            MongoClient client = await MongoDbConnection.AuthenticateMongoClientAsync(cosmosDbResourceId, databaseName, collectionName, logger);
            IMongoDatabase database = client.GetDatabase(databaseName);

            return await CreateIfNotExistsAsync(database, collectionName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos MongoDb collection if it doesn't exist yet.
        /// </summary>
        /// <param name="database">The client to the MongoDb database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="database"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(IMongoDatabase database, string collectionName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(database, collectionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryMongoDbCollection"/> which creates a new Azure Cosmos MongoDb collection if it doesn't exist yet.
        /// </summary>
        /// <param name="database">The client to the MongoDb database in which the collection should be created.</param>
        /// <param name="collectionName">The unique name of the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb collection.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="database"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbCollection> CreateIfNotExistsAsync(
            IMongoDatabase database,
            string collectionName,
            ILogger logger,
            Action<TemporaryMongoDbCollectionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(database);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Cosmos MongoDb collection name to create a temporary collection");
            }

            var options = new TemporaryMongoDbCollectionOptions();
            configureOptions?.Invoke(options);

            var listOptions = new ListCollectionNamesOptions
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName)
            };
            using IAsyncCursor<string> collectionNames = await database.ListCollectionNamesAsync(listOptions);
            if (!await collectionNames.AnyAsync())
            {
                logger.LogTrace("Creating Azure Cosmos MongoDb '{CollectionName}' collection in database '{DatabaseName}'", collectionName, database.DatabaseNamespace.DatabaseName);
                await database.CreateCollectionAsync(collectionName);

                return new TemporaryMongoDbCollection(createdByUs: true, collectionName, database, logger, options);
            }

            logger.LogTrace("Azure Cosmos MongoDb '{CollectionName}' collection in database '{DatabaseName}' already exists", collectionName, database.DatabaseNamespace.DatabaseName);

            await CleanCollectionOnSetupAsync(database, collectionName, options, logger);
            return new TemporaryMongoDbCollection(createdByUs: false, collectionName, database, logger, options);
        }

        private static async Task CleanCollectionOnSetupAsync(IMongoDatabase database, string collectionName, TemporaryMongoDbCollectionOptions options, ILogger logger)
        {
            if (options.OnSetup.Documents is OnSetupMongoDbCollection.LeaveExisted)
            {
                return;
            }

            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

            if (options.OnSetup.Documents is OnSetupMongoDbCollection.CleanIfExisted)
            {
                logger.LogTrace("Cleaning all documents in Azure Cosmos MongoDb collection '{CollectionName}'", collectionName);
                await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
            }

            if (options.OnSetup.Documents is OnSetupMongoDbCollection.CleanIfMatched)
            {
                logger.LogTrace("Clean all matching documents in Azure Cosmos MongoDb collection '{CollectionName}'", collectionName);
                await collection.DeleteManyAsync(options.OnSetup.IsMatched);
            }
        }

        /// <summary>
        /// Inserts a temporary <paramref name="document"/> to the MongoDb collection.
        /// </summary>
        /// <remarks>
        ///     All documents inserted via the temporary collection will always be removed when the collection disposes,
        ///     regardless of what teardown options were configured on the collection.
        /// </remarks>
        /// <typeparam name="TDocument">The type of the document in the MongoDb collection.</typeparam>
        /// <param name="document">The document to upload to the MongoDb collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="document"/> is <c>null</c>.</exception>
        public async Task InsertDocumentAsync<TDocument>(TDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            IMongoCollection<TDocument> collection = GetCollectionClient<TDocument>();
            _documents.Add(await TemporaryMongoDbDocument.InsertIfNotExistsAsync(collection, document, _logger));
        }

        /// <summary>
        /// Gets the client to interact with the collection that is temporary available.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document in the MongoDb collection.</typeparam>
        public IMongoCollection<TDocument> GetCollectionClient<TDocument>()
        {
            return _database.GetCollection<TDocument>(_collectionName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            disposables.AddRange(_documents);

            if (_createdByUs)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Drop Azure Cosmos MongoDb '{CollectionName}' collection from database '{DatabaseName}'", _collectionName, _database.DatabaseNamespace.DatabaseName);
                    await _database.DropCollectionAsync(_collectionName); 
                }));
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await CleanCollectionOnTeardownAsync();
                }));
            }
        }

        private async Task CleanCollectionOnTeardownAsync()
        {
            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanIfCreated)
            {
                return;
            }

            IMongoCollection<BsonDocument> collection = _database.GetCollection<BsonDocument>(_collectionName);

            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanAll)
            {
                _logger.LogTrace("Clean all documents in Azure Cosmos MongoDb '{CollectionName}' collection", _collectionName);
                await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
            }

            if (_options.OnTeardown.Documents is OnTeardownMongoDbCollection.CleanIfMatched)
            {
                _logger.LogTrace("Clean all matching documents in Azure Cosmos MongoDb '{CollectionName}' collection", _collectionName);
                await collection.DeleteManyAsync(_options.OnTeardown.IsMatched);
            }
        }
    }
}
