using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using static Arcus.Testing.NoSqlExtraction;
using CosmosException = Microsoft.Azure.Cosmos.CosmosException;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure CosmosDb NoSql document that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryNoSqlItem : IAsyncDisposable
    {
        private readonly Container _container;
        private readonly Type _itemType;
        private readonly bool _createdByUs;
        private readonly Stream _originalItemStream;
        private readonly ILogger _logger;

        private TemporaryNoSqlItem(
            Container container,
            string itemId,
            PartitionKey partitionKey,
            Type itemType,
            bool createdByUs,
            Stream originalItemStream,
            ILogger logger)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _itemType = itemType;
            _createdByUs = createdByUs;
            _originalItemStream = originalItemStream;
            _logger = logger ?? NullLogger.Instance;

            Id = itemId ?? throw new ArgumentNullException(nameof(itemId));
            PartitionKey = partitionKey;
        }

        /// <summary>
        /// Gets the unique identifier of this temporary NoSql item.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the key of this temporary NoSql item in which the data is partitioned.
        /// </summary>
        public PartitionKey PartitionKey { get; }

        /// <summary>
        /// Creates a temporary item to a NoSql container.
        /// </summary>
        /// <param name="container">The NoSql container where a temporary item should be created.</param>
        /// <param name="item">The item to temporary create in the NoSql container.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="container"/> is <c>null</c>.</exception>
        public static async Task<TemporaryNoSqlItem> InsertIfNotExistsAsync<TItem>(Container container, TItem item, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(container);
            logger ??= NullLogger.Instance;

            JObject json = JObject.FromObject(item);
            string itemId = ExtractIdFromItem(json, typeof(TItem));

            ContainerResponse resp = await container.ReadContainerAsync();
            PartitionKey partitionKey = ExtractPartitionKeyFromItem(resp.Resource, json);

            try
            {
                return await ReplaceExistingItemAsync(container, itemId, partitionKey, item, logger);
            }
            catch (CosmosException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
            {
                return await InsertNewItemAsync(container, itemId, partitionKey, item, logger);
            }
        }

        private static async Task<TemporaryNoSqlItem> ReplaceExistingItemAsync<TItem>(
            Container container,
            string itemId,
            PartitionKey partitionKey,
            TItem item,
            ILogger logger)
        {
            ItemResponse<TItem> response = await container.ReadItemAsync<TItem>(itemId, partitionKey);
            logger.LogTrace("Replace '{ItemType}' NoSql item'{ItemId}' in Azure CosmosDb container '{ContainerName}'", typeof(TItem).Name, itemId, container.Id);
            
            CosmosSerializer serializer = container.Database.Client.ClientOptions.Serializer;
            if (serializer is null)
            {
                throw new InvalidOperationException(
                    "Cannot temporary insert a NoSql item in a NoSql container because no JSON serializer was set in the Cosmos client options");
            }

            TItem original = response.Resource;
            var originalItemStream = serializer.ToStream(original);

            await container.ReplaceItemAsync(item, itemId, partitionKey);
            return new TemporaryNoSqlItem(container, itemId, partitionKey, typeof(TItem), createdByUs: false, originalItemStream, logger);
        }

        private static async Task<TemporaryNoSqlItem> InsertNewItemAsync<TItem>(
            Container container,
            string itemId,
            PartitionKey partitionKey,
            TItem item,
            ILogger logger)
        {
            logger.LogTrace("Inserting new '{ItemType}' NoSql item '{ItemId}' to Azure CosmosDb container '{ContainerName}'", typeof(TItem).Name, itemId, container.Id);
            ItemResponse<TItem> response = await container.CreateItemAsync(item);
            
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new RequestFailedException((int)response.StatusCode, response.Diagnostics.ToString());
            }

            return new TemporaryNoSqlItem(container, itemId, partitionKey, typeof(TItem), createdByUs: true, originalItemStream: null, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (_createdByUs && _originalItemStream is null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Deleting '{ItemType}' NoSql item '{ItemId}' in Azure CosmosDb NoSql container '{ContainerName}'", _itemType.Name, Id, _container.Id);
                    await _container.DeleteItemStreamAsync(Id, PartitionKey);
                }));
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("Reverting '{ItemType}' NoSql item '{ItemId}' in Azure CosmosDb NoSql container '{ContainerName}'", _itemType.Name, Id, _container.Id);
                    await _container.ReplaceItemStreamAsync(_originalItemStream, Id, PartitionKey);
                }));
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await _originalItemStream.DisposeAsync();
                }));
            }
        }
    }
}
