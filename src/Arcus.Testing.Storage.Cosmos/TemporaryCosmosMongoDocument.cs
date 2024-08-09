using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Arcus.Testing
{
    /// <summary>
    /// 
    /// </summary>
    public static class TemporaryCosmosMongoDocument
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TDocument"></typeparam>
        /// <returns></returns>
        public static async Task<TemporaryCosmosMongoDbDocument<TDocument>> UploadIfNotExistsAsync<TDocument>(
            IMongoCollection<TDocument> collection,
            TDocument document,
            ILogger logger)
        {
            await collection.InsertOneAsync(document);
            collection.dele
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class TemporaryCosmosMongoDbDocument<TDocument> : IAsyncDisposable
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly string _id;
        private readonly ILogger _logger;

        internal TemporaryCosmosMongoDbDocument(IMongoCollection<TDocument> collection, string id, ILogger logger)
        {
            _collection = collection;
            _id = id;
            _logger = logger;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("Delete Azure Cosmos MongoDb {DocumentType} document '{DocumentId}' (Partition: {PartitionKey}) in collection '{CollectionName}'", typeof(TDocument).Name, _id, _collection.);
        }
    }
}
