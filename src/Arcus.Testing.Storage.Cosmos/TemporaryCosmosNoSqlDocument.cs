using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary NoSql document in the Azure Cosmos DB resource that will be deleted after the instance is disposed.
    /// </summary>
    public static class TemporaryCosmosNoSqlDocument
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="partitionKey"></param>
        /// <param name="item">A JSON serializable object that must contain an id property. <see cref="T:Microsoft.Azure.Cosmos.CosmosSerializer" /> to implement a custom serializer</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<TemporaryCosmosNoSqlDocument<TDocument>> UploadIfNotExistsAsync<TDocument>(
            Container container,
            PartitionKey partitionKey,
            TDocument item,
            ILogger logger)
        {
            JToken json = JToken.FromObject(item);
            string id = json["id"].Value<string>();

            ItemResponse<TDocument> response = await container.UpsertItemAsync(item, partitionKey);
            
        }
    }

    /// <summary>
    /// Represents a temporary NoSql document in the Azure Cosmos DB resource that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryCosmosNoSqlDocument<TDocumentType> : IAsyncDisposable
    {
        private readonly string _id;
        private readonly PartitionKey _partitionKey;
        private readonly Container _container;
        private readonly ILogger _logger;

        internal TemporaryCosmosNoSqlDocument(Container container, PartitionKey partitionKey, string id, ILogger logger)
        {
            _container = container;
            _partitionKey = partitionKey;
            _id = id;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("Delete Azure Cosmos NoSql {DocumentType} document '{DocumentId}' (Partition: {PartitionKey}) in container '{ContainerId}'", typeof(TDocumentType).Name, _id, _partitionKey, _container.Id);
            ItemResponse<TDocumentType> response = await _container.DeleteItemAsync<TDocumentType>(_id, _partitionKey);
        }
    }
}
