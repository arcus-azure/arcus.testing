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

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Cosmos NoSql document that will be deleted after the instance is disposed.
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
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(itemType);
            ArgumentNullException.ThrowIfNull(itemId);

            _container = container;
            _itemType = itemType;
            _createdByUs = createdByUs;
            _originalItemStream = originalItemStream;
            _logger = logger ?? NullLogger.Instance;

            Id = itemId;
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
        /// Creates a temporary item to an Azure Cosmos NoSql container.
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
            PartitionKey partitionKey = ExtractPartitionKeyFromItem(json, resp.Resource);

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
            logger.LogTrace("[Test:Setup] Try replacing Azure Cosmos NoSql '{ItemType}' item '{ItemId}' in container '{DatabaseName}/{ContainerName}'...", typeof(TItem).Name, itemId, container.Database.Id, container.Id);
            ItemResponse<TItem> response = await container.ReadItemAsync<TItem>(itemId, partitionKey);

            CosmosSerializer serializer = container.Database.Client.ClientOptions.Serializer;
            if (serializer is null)
            {
                throw new InvalidOperationException(
                    "[Test:Setup] Cannot temporary insert an Azure Cosmos NoSql item in a NoSql container because no JSON serializer was set in the Cosmos client options");
            }

            TItem original = response.Resource;
            var originalItemStream = serializer.ToStream(original);

            logger.LogDebug("[Test:Setup] Replace Azure Cosmos NoSql '{ItemType}' item '{ItemId}' in container '{DatabaseName}/{ContainerName}'", typeof(TItem).Name, itemId, container.Database.Id, container.Id);
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
            logger.LogDebug("[Test:Setup] Insert new Azure Cosmos NoSql '{ItemType}' item '{ItemId}' to container '{DatabaseName}/{ContainerName}'", typeof(TItem).Name, itemId, container.Database.Id, container.Id);
            ItemResponse<TItem> response = await container.CreateItemAsync(item);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new RequestFailedException(
                    (int) response.StatusCode, $"[Test:Setup] Unable to insert a new Azure Cosmos NoSql '{typeof(TItem).Name}' item into the container '{container.Database.Id}/{container.Id}': {response.StatusCode} - " + response.Diagnostics);
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
                    _logger.LogDebug("[Test:Teardown] Delete Azure Cosmos NoSql '{ItemType}' item '{ItemId}' in container '{DatabaseName}/{ContainerName}'", _itemType.Name, Id, _container.Database.Id, _container.Id);
                    using ResponseMessage response = await _container.DeleteItemStreamAsync(Id, PartitionKey);

                    if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw new RequestFailedException(
                            $"[Test:Teardown] Failed to delete Azure Cosmos NoSql '{_itemType.Name}' item '{Id}' {PartitionKey} in container '{_container.Database.Id}/{_container.Id}' " +
                            $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                    }
                }));
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogDebug("[Test:Teardown] Revert replaced Azure Cosmos NoSql '{ItemType}' item '{ItemId}' in container '{DatabaseName}/{ContainerName}'", _itemType.Name, Id, _container.Database.Id, _container.Id);
                    using ResponseMessage response = await _container.ReplaceItemStreamAsync(_originalItemStream, Id, PartitionKey);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new RequestFailedException(
                            $"[Test:Teardown] Failed to revert Azure Cosmos NoSql '{_itemType.Name}' item '{Id}' {PartitionKey} in container '{_container.Database.Id}/{_container.Id}' " +
                            $"since the replace operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                    }
                }));
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await _originalItemStream.DisposeAsync();
                }));
            }

            GC.SuppressFinalize(this);
        }
    }
}
