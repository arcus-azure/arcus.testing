using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Represents a temporary Azure Cosmos MongoDb document that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryMongoDbDocument : IAsyncDisposable
    {
        private readonly FilterDefinition<BsonDocument> _filter;
        private readonly Type _documentType;
        private readonly BsonDocument _originalDoc;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        private TemporaryMongoDbDocument(
            BsonValue id,
            Type documentType,
            FilterDefinition<BsonDocument> filter,
            BsonDocument originalDoc,
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
            _collection = collection;
            _logger = logger ?? NullLogger.Instance;

            Id = id;
        }

        /// <summary>
        /// Gets the current ID of the document.
        /// </summary>
        public BsonValue Id { get; }

        /// <summary>
        /// Inserts a temporary document to an Azure Cosmos MongoDb collection.
        /// </summary>
        /// <param name="cosmosDbResourceId">The resource ID pointing towards the Azure CosmosDb account.</param>
        /// <param name="databaseName">The name of the MongoDb database in which the collection resides where the document should be created.</param>
        /// <param name="collectionName">The name of the MongoDb collection in which the document should be created.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbResourceId"/> or <paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName"/> or the <paramref name="collectionName"/> is blank.</exception>
        public static async Task<TemporaryMongoDbDocument> InsertIfNotExistsAsync<TDocument>(
            ResourceIdentifier cosmosDbResourceId,
            string databaseName,
            string collectionName,
            TDocument document,
            ILogger logger)
            where TDocument : class
        {
            ArgumentNullException.ThrowIfNull(cosmosDbResourceId);
            ArgumentNullException.ThrowIfNull(document);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException(
                    "Requires a non-blank MongoDb database name to insert a temporary document in a collection", nameof(databaseName));
            }

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException(
                    "Requires a non-blank MongoDb collection name to insert a temporary document", nameof(collectionName));
            }


            MongoClient client = await MongoDbConnection.AuthenticateMongoClientAsync(cosmosDbResourceId, databaseName, collectionName, logger);
            IMongoDatabase database = client.GetDatabase(databaseName);
            IMongoCollection<TDocument> collection = database.GetCollection<TDocument>(collectionName);

            return await InsertIfNotExistsAsync(collection, document, logger);
        }

        /// <summary>
        /// Inserts a temporary document to an Azure Cosmos MongoDb collection.
        /// </summary>
        /// <param name="collection">The collection client to interact with the MongoDb collection.</param>
        /// <param name="document">The document that should be temporarily inserted into the MongoDb collection.</param>
        /// <param name="logger">The logger to write diagnostic information during the lifetime of the MongoDb document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="collection"/> or the <paramref name="document"/> is <c>null</c>.</exception>
        public static async Task<TemporaryMongoDbDocument> InsertIfNotExistsAsync<TDocument>(IMongoCollection<TDocument> collection, TDocument document, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(document);
            logger ??= NullLogger.Instance;

            IMongoCollection<BsonDocument> collectionBson =
                collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);

            BsonMemberMap idMemberMap = DetermineIdMemberMap(collection);
            var bson = document.ToBsonDocument();
            BsonValue id = DetermineId(collection, bson, idMemberMap);

            FilterDefinition<BsonDocument> findOneDocument = Builders<BsonDocument>.Filter.Eq(idMemberMap.ElementName, id);
            using IAsyncCursor<BsonDocument> existingDocs = await collectionBson.FindAsync(findOneDocument);

            List<BsonDocument> matchingDocs = await existingDocs.ToListAsync();
            if (matchingDocs.Count > 1)
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert a document in the MongoDb collection '{collection.CollectionNamespace.FullName}' " +
                    $"as the passed filter expression for document type '{typeof(TDocument).Name}' matches more than a single document, " +
                    $"please stricken the filter expression so that it only matches a single document, if you want to temporary replace the document");
            }

            if (matchingDocs.Count is 0)
            {
                logger.LogDebug("[Test:Setup] Insert new Azure Cosmos MongoDb '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'", typeof(TDocument).Name, BsonTypeMapper.MapToDotNetValue(id), collection.Database.DatabaseNamespace.DatabaseName, collection.CollectionNamespace.FullName);

                await collectionBson.InsertOneAsync(bson);
                return new TemporaryMongoDbDocument(id, typeof(TDocument), findOneDocument, originalDoc: null, collectionBson, logger);
            }

            logger.LogDebug("[Test:Setup] Replace Azure Cosmos MongoDb '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'", typeof(TDocument).Name, BsonTypeMapper.MapToDotNetValue(id), collection.Database.DatabaseNamespace.DatabaseName, collection.CollectionNamespace.FullName);

            BsonDocument originalDoc = matchingDocs.Single();
            await collectionBson.FindOneAndReplaceAsync(findOneDocument, bson);

            return new TemporaryMongoDbDocument(id, typeof(TDocument), findOneDocument, originalDoc, collectionBson, logger);
        }

        private static BsonMemberMap DetermineIdMemberMap<TDocument>(IMongoCollection<TDocument> collection)
        {
            BsonClassMap classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
            if (classMap.IdMemberMap is null)
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert a document in the MongoDb collection '{collection.CollectionNamespace.FullName}' " +
                    $"as the passed document type '{typeof(TDocument).Name}' has no member map defined for the '_id' property, " +
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
            await using var disposables = new DisposableCollection(_logger);

            object id = BsonTypeMapper.MapToDotNetValue(Id);
            if (_originalDoc is null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogDebug("[Test:Teardown] Delete Azure Cosmos MongoDb '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'", _documentType.Name, id, _collection.Database.DatabaseNamespace.DatabaseName, _collection.CollectionNamespace.FullName);
                    await _collection.DeleteOneAsync(_filter);
                }));
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogDebug("[Test:Teardown] Revert replaced Azure Cosmos MongoDb '{DocumentType}' document '{DocumentId}' in collection '{DatabaseName}/{CollectionName}'", _documentType.Name, id, _collection.Database.DatabaseNamespace.DatabaseName, _collection.CollectionNamespace.FullName);
                    await _collection.ReplaceOneAsync(_filter, _originalDoc);
                }));
            }

            GC.SuppressFinalize(this);
        }
    }
}
