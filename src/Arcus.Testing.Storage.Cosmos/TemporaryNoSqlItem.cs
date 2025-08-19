using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Arcus.Testing.NoSqlExtraction;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Cosmos DB for NoSQL document that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryNoSqlItem : IAsyncDisposable
    {
        private readonly Container _container;
        private readonly Type _itemType;
        private readonly bool _createdByUs;
#pragma warning disable CA2213 // Disposable field is disposed via the disposable collection.
        private readonly Stream _originalItemStream;
#pragma warning restore CA2213
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
        /// Gets the unique identifier of this temporary NoSQL item.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the key of this temporary NoSql item in which the data is partitioned.
        /// </summary>
        public PartitionKey PartitionKey { get; }

        /// <summary>
        /// Creates a temporary item to an Azure Cosmos DB for NoSQL container.
        /// </summary>
        /// <param name="container">The NoSQL container where a temporary item should be created.</param>
        /// <param name="item">The item to temporary create in the NoSQL container.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="container"/> is <c>null</c>.</exception>
#pragma warning disable S1133 // Will be deleted in v3.0.
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertItemAsync) + " instead which provides the exact same functionality")]
#pragma warning restore S1133
        public static Task<TemporaryNoSqlItem> InsertIfNotExistsAsync<TItem>(Container container, TItem item, ILogger logger)
        {
            return UpsertItemAsync(container, item, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing NoSQL item in an Azure Cosmos DB for NoSQL container.
        /// </summary>
        /// <remarks>
        ///     ⚡ Item will be deleted (if new) or reverted (if existing) when the <see cref="TemporaryNoSqlItem"/> is disposed.
        /// </remarks>
        /// <param name="container">The NoSQL container where a temporary item should be created.</param>
        /// <param name="item">The item to temporary create in the NoSQL container.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="container"/> is <c>null</c>.</exception>
        public static async Task<TemporaryNoSqlItem> UpsertItemAsync<TItem>(Container container, TItem item, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(container);
            logger ??= NullLogger.Instance;
            Type itemType = typeof(TItem);

            JsonNode json = JsonSerializer.SerializeToNode(item, SerializeToNodeOptions);
            string itemId = ExtractIdFromItem(json, itemType);

            ContainerResponse resp = await container.ReadContainerAsync().ConfigureAwait(false);
            PartitionKey partitionKey = ExtractPartitionKeyFromItem(json, resp.Resource);

            try
            {
                Stream originalItemStream = await ReplaceExistingItemOnSetupAsync(container, itemType, itemId, partitionKey, item, logger).ConfigureAwait(false);
                return new TemporaryNoSqlItem(container, itemId, partitionKey, itemType, createdByUs: false, originalItemStream, logger);
            }
            catch (CosmosException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
            {
                await InsertNewItemOnSetupAsync(container, itemType, itemId, item, logger).ConfigureAwait(false);
                return new TemporaryNoSqlItem(container, itemId, partitionKey, itemType, createdByUs: true, originalItemStream: null, logger);
            }
        }

        private static async Task<Stream> ReplaceExistingItemOnSetupAsync<TItem>(
            Container container,
            Type itemType,
            string itemId,
            PartitionKey partitionKey,
            TItem item,
            ILogger logger)
        {
            logger.LogBeforeSetupReplaceItem(itemType.Name, itemId, container.Database.Id, container.Id);
            ItemResponse<TItem> response = await container.ReadItemAsync<TItem>(itemId, partitionKey).ConfigureAwait(false);

            CosmosSerializer serializer = container.Database.Client.ClientOptions.Serializer;
            if (serializer is null)
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert an NoSQL item in the Azure Cosmos DB for NoSQL container '{container.Database.Id}/{container.Id}' because no JSON serializer was set in the Cosmos DB client options");
            }

            TItem original = response.Resource;
            var originalItemStream = serializer.ToStream(original);

            logger.LogSetupReplaceItem(itemType.Name, itemId, container.Database.Id, container.Id);
            await container.ReplaceItemAsync(item, itemId, partitionKey).ConfigureAwait(false);

            return originalItemStream;
        }

        private static async Task InsertNewItemOnSetupAsync<TItem>(Container container, Type itemType, string itemId, TItem item, ILogger logger)
        {
            logger.LogSetupInsertNewItem(itemType.Name, itemId, container.Database.Id, container.Id);
            ItemResponse<TItem> response = await container.CreateItemAsync(item).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new RequestFailedException((int) response.StatusCode,
                    $"[Test:Setup] Failed to insert a new NoSQL '{itemType.Name}' item into the Azure Cosmos DB for NoSQL container '{container.Database.Id}/{container.Id}' " +
                    $"since the create operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.Diagnostics}");
            }
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
                if (_createdByUs && _originalItemStream is null)
                {
                    disposables.Add(AsyncDisposable.Create(DeleteItemOnTeardownAsync));
                }
                else
                {
                    disposables.Add(AsyncDisposable.Create(RevertItemOnTeardownAsync));
                    disposables.Add((IAsyncDisposable) _originalItemStream);
                }

                GC.SuppressFinalize(this);
            }
        }

        private async Task DeleteItemOnTeardownAsync()
        {
            _logger.LogTeardownDeleteItem(_itemType.Name, Id, _container.Database.Id, _container.Id);
            using ResponseMessage response = await _container.DeleteItemStreamAsync(Id, PartitionKey).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new RequestFailedException(
                    (int) response.StatusCode,
                    $"[Test:Teardown] Failed to delete NoSQL '{_itemType.Name}' item '{Id}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{_container.Database.Id}/{_container.Id}' " +
                    $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
            }
        }

        private async Task RevertItemOnTeardownAsync()
        {
            _logger.LogTeardownRevertItem(_itemType.Name, Id, _container.Database.Id, _container.Id);
            using ResponseMessage response = await _container.ReplaceItemStreamAsync(_originalItemStream, Id, PartitionKey).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(
                    (int) response.StatusCode,
                    $"[Test:Teardown] Failed to revert NoSQL '{_itemType.Name}' item '{Id}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{_container.Database.Id}/{_container.Id}' " +
                    $"since the replace operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
            }
        }
    }

    internal static partial class TempNoSqlItemILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Insert new NoSQL '{ItemType}' item '{ItemId}' to Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogSetupInsertNewItem(this ILogger logger, string itemType, string itemId, string databaseName, string containerName);

        [LoggerMessage(
            Level = LogLevel.Trace,
            Message = "[Test:Setup] Try replacing NoSQL '{ItemType}' item '{ItemId}' in Azure Cosmos DB For NoSQL container '{DatabaseName}/{ContainerName}'...")]
        internal static partial void LogBeforeSetupReplaceItem(this ILogger logger, string itemTYpe, string itemId, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Replace NoSQL '{ItemType}' item '{ItemId}' in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogSetupReplaceItem(this ILogger logger, string itemType, string itemId, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete NoSQL '{ItemType}' item '{ItemId}' in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogTeardownDeleteItem(this ILogger logger, string itemType, string itemId, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert replaced NoSQL '{ItemType}' item '{ItemId}' in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogTeardownRevertItem(this ILogger logger, string itemType, string itemId, string databaseName, string containerName);
    }
}
