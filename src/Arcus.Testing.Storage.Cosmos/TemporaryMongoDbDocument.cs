using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Represents a temporary Azure Cosmos DB for MongoDB document that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryMongoDbDocument : IAsyncDisposable
    {
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly Type _documentType;
        private readonly BsonDocument _originalDoc;
        private readonly MongoClient _client;
        private readonly bool _clientCreatedByUs;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        private TemporaryMongoDbDocument(
            BsonValue id,
            Type documentType,
            FilterDefinition<BsonDocument> filter,
            BsonDocument originalDoc,
            MongoClient client,
            bool clientCreatedByUs,
            IMongoCollection<BsonDocument> collection,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(documentType);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(filter);

            _documentType = documentType;
            _filter = filter;
            _originalDoc = originalDoc;
            _client = client;
            _clientCreatedByUs = clientCreatedByUs;
            _collection = collection;
            _logger = logger ?? NullLogger.Instance;

            Id = id;
        }

        /// <summary>
        /// Gets the current ID of the MongoDB document.
        /// </summary>
        public BsonValue Id { get; }

        /// <summary>
        /// Inserts a temporary document to an Azure Cosmos DB for MongoDB collection.
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
        /// <param name="databaseName">The name of the MongoDB database in which the collection resides where the document should be created.</param>
        /// <param name="collectionName">The name of the MongoDB collection in which the document should be created.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> or <paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or the <paramref name="collectionName"/> is blank.</exception>
        [Obsolete("Will be removed in v3, please use the " + nameof(UpsertDocumentAsync) + "instead that provides exactly the same functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public static Task<TemporaryMongoDbDocument> InsertIfNotExistsAsync<TDocument>(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            TDocument document,
            ILogger logger)
            where TDocument : class
        {
            return UpsertDocumentAsync(cosmosDbResourceId, databaseName, collectionName, document, logger);
        }

        /// <summary>
        /// Inserts a temporary document to an Azure Cosmos DB for MongoDB collection.
        /// </summary>
        /// <param name="collection">The collection client to interact with the MongoDB collection.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="document"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3, please use the " + nameof(UpsertDocumentAsync) + "instead that provides exactly the same functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public static Task<TemporaryMongoDbDocument> InsertIfNotExistsAsync<TDocument>(IMongoCollection<TDocument> collection, TDocument document, ILogger logger)
        {
            return UpsertDocumentAsync(collection, document, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing document in an Azure Cosmos DB for MongoDB collection.
        /// </summary>
        /// <remarks>
        ///     ⚡ Document will be deleted (if new) or reverted (if existing) when the <see cref="TemporaryMongoDbDocument"/> is disposed.
        /// </remarks>
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
        /// <param name="databaseName">The name of the MongoDB database in which the collection resides where the document should be created.</param>
        /// <param name="collectionName">The name of the MongoDB collection in which the document should be created.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> or <paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or the <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbDocument> UpsertDocumentAsync<TDocument>(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            TDocument document,
            ILogger logger)
            where TDocument : class
        {
            ArgumentNullException.ThrowIfNull(cosmosDbResourceId);
            ArgumentNullException.ThrowIfNull(document);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
            logger ??= NullLogger.Instance;

            MongoClient client = await MongoDbConnection.AuthenticateMongoClientAsync(cosmosDbResourceId, databaseName, collectionName, logger).ConfigureAwait(false);
            IMongoDatabase database = client.GetDatabase(databaseName);
            IMongoCollection<TDocument> collection = database.GetCollection<TDocument>(collectionName);

            return await UpsertDocumentAsync(client, clientCreatedByUs: true, collection, document, logger).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new or replaces an existing document in an Azure Cosmos DB for MongoDB collection.
        /// </summary>
        /// <remarks>
        ///     ⚡ Document will be deleted (if new) or reverted (if existing) when the <see cref="TemporaryMongoDbDocument"/> is disposed.
        /// </remarks>
        /// <param name="collection">The collection client to interact with the MongoDB collection.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDB collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDB document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="document"/> is <c>null</c>.</exception>
        public static Task<TemporaryMongoDbDocument> UpsertDocumentAsync<TDocument>(IMongoCollection<TDocument> collection, TDocument document, ILogger logger)
        {
            return UpsertDocumentAsync(client: null, clientCreatedByUs: false, collection, document, logger);
        }

        private static async Task<TemporaryMongoDbDocument> UpsertDocumentAsync<TDocument>(
            MongoClient client, bool clientCreatedByUs,
            IMongoCollection<TDocument> collection,
            TDocument document,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(document);
            logger ??= NullLogger.Instance;

            Type documentType = typeof(TDocument);
            var bson = document.ToBsonDocument();

            BsonMemberMap idMemberMap = DetermineIdMemberMap(collection, documentType);
            BsonValue id = DetermineId(collection, bson, idMemberMap);
            FilterDefinition<BsonDocument> findOneDocument = Builders<BsonDocument>.Filter.Eq(idMemberMap.ElementName, id);
            object documentId = BsonTypeMapper.MapToDotNetValue(id);

            string collectionName = collection.CollectionNamespace.CollectionName;
            IMongoCollection<BsonDocument> collectionBson = collection.Database.GetCollection<BsonDocument>(collectionName);

            BsonDocument originalDoc = await FindOriginalDocumentAsync(collectionBson, findOneDocument, documentType).ConfigureAwait(false);
            if (originalDoc is null)
            {
                logger.LogSetupInsertNewDocument(documentType.Name, documentId, collection.Database.DatabaseNamespace.DatabaseName, collectionName);

                await collectionBson.InsertOneAsync(bson).ConfigureAwait(false);
                return new(id, documentType, findOneDocument, originalDoc: null, client, clientCreatedByUs, collectionBson, logger);
            }

            logger.LogSetupReplaceDocument(documentType.Name, documentId, collection.Database.DatabaseNamespace.DatabaseName, collectionName);
            await collectionBson.FindOneAndReplaceAsync(findOneDocument, bson).ConfigureAwait(false);

            return new TemporaryMongoDbDocument(id, documentType, findOneDocument, originalDoc, client, clientCreatedByUs, collectionBson, logger);
        }

        private static async Task<BsonDocument> FindOriginalDocumentAsync(
            IMongoCollection<BsonDocument> collectionBson,
            FilterDefinition<BsonDocument> findOneDocument,
            Type documentType)
        {
            using IAsyncCursor<BsonDocument> existingDocs = await collectionBson.FindAsync(findOneDocument).ConfigureAwait(false);
            List<BsonDocument> matchingDocs = await existingDocs.ToListAsync().ConfigureAwait(false);

            if (matchingDocs.Count > 1)
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert a document in the Azure Cosmos DB for MongoDB collection '{collectionBson.CollectionNamespace.FullName}' " +
                    $"as the passed filter expression for document type '{documentType.Name}' matches more than a single document, " +
                    $"please stricken the filter expression so that it only matches a single document, if you want to temporary replace the document");
            }

            if (matchingDocs.Count is 0)
            {
                return null;
            }

            return matchingDocs.Single();
        }

        private static BsonMemberMap DetermineIdMemberMap<TDocument>(IMongoCollection<TDocument> collection, Type documentType)
        {
            BsonClassMap classMap = BsonClassMap.LookupClassMap(documentType);
            if (classMap.IdMemberMap is null)
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert a document in the Azure Cosmos DB for MongoDB collection '{collection.CollectionNamespace.FullName}' " +
                    $"as the passed document type '{documentType.Name}' has no member map defined for the '_id' property, " +
                    $"please see the MongoDb documentation for more information on this required property: https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/crud/write-operations/insert/#the-_id-field");
            }

            return classMap.IdMemberMap;
        }

        private static BsonValue DetermineId<TDocument>(IMongoCollection<TDocument> collection, BsonDocument bson, BsonMemberMap idMemberMap)
        {
            BsonValue id = bson[idMemberMap.ElementName];
            object idValue = BsonTypeMapper.MapToDotNetValue(id);

            if (idMemberMap.IdGenerator.IsEmpty(idValue))
            {
                object newId = idMemberMap.IdGenerator.GenerateId(collection, bson);
                id = BsonTypeMapper.MapToBsonValue(newId);

                bson[idMemberMap.ElementName] = id;
            }

            return id;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            var disposables = new DisposableCollection(_logger);
            await using (disposables.ConfigureAwait(false))
            {
                object id = BsonTypeMapper.MapToDotNetValue(Id);
                if (_originalDoc is null)
                {
                    disposables.Add(AsyncDisposable.Create(() =>
                    {
                        _logger.LogTeardownDeleteDocument(_documentType.Name, id, _collection.Database.DatabaseNamespace.DatabaseName, _collection.CollectionNamespace.CollectionName);
                        return _collection.DeleteOneAsync(_filter);
                    }));
                }
                else
                {
                    disposables.Add(AsyncDisposable.Create(() =>
                    {
                        _logger.LogTeardownRevertDocument(_documentType.Name, id, _collection.Database.DatabaseNamespace.DatabaseName, _collection.CollectionNamespace.CollectionName);
                        return _collection.ReplaceOneAsync(_filter, _originalDoc);
                    }));
                }

                if (_clientCreatedByUs && _client != null)
                {
                    disposables.Add(_client);
                }

                GC.SuppressFinalize(this);
            }
        }
    }

    internal static partial class TempMongoDbDocumentILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Insert new Azure Cosmos DB for MongoDB '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogSetupInsertNewDocument(this ILogger logger, string documentType, object documentId, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Replace Azure Cosmos DB for MongoDB '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogSetupReplaceDocument(this ILogger logger, string documentType, object documentId, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Cosmos DB for MongoDB '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogTeardownDeleteDocument(this ILogger logger, string documentType, object documentId, string databaseName, string collectionName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert replaced Azure Cosmos DB for MongoDB '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'")]
        internal static partial void LogTeardownRevertDocument(this ILogger logger, string documentType, object documentId, string databaseName, string collectionName);
    }
}
