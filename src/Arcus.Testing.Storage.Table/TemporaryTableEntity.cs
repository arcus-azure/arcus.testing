using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Table entity that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryTableEntity : IAsyncDisposable
    {
        private const int NotFound = 404;

        private readonly TableClient _tableClient;
        private readonly Type _entityType;
        private readonly bool _createdByUs;
        private readonly ITableEntity _entity;
        private readonly ILogger _logger;

        private TemporaryTableEntity(
            TableClient tableClient,
            Type entityType,
            bool createdByUs,
            ITableEntity originalEntity,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(tableClient);
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(originalEntity);

            _tableClient = tableClient;
            _entityType = entityType;
            _createdByUs = createdByUs;
            _entity = originalEntity;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates a temporary entity in an Azure Table.
        /// </summary>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="accountName">The name of the Azure Storage account where the table for the entity is located.</param>
        /// <param name="tableName">The name of the Azure Table where the table entity should be added.</param>
        /// <param name="entity">The entity to add to the Azure Table.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or the <paramref name="tableName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is <c>null</c>.</exception>
#pragma warning disable S1133 // Will be removed in v3.0.
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertEntityAsync) + " instead which provides exactly the same functionality")]
#pragma warning restore S1133
        public static Task<TemporaryTableEntity> AddIfNotExistsAsync<TEntity>(
            string accountName,
            string tableName,
            TEntity entity,
            ILogger logger)
            where TEntity : class, ITableEntity
        {
            return UpsertEntityAsync(accountName, tableName, entity, logger);
        }

        /// <summary>
        /// Creates a temporary entity in an Azure Table.
        /// </summary>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="client">The client to interact with the Azure Table resource.</param>
        /// <param name="entity">The entity to add to the Azure Table.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> or the <paramref name="entity"/> is <c>null</c>.</exception>
#pragma warning disable S1133 // Will be removed in v3.0.
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertEntityAsync) + " instead which provides exactly the same functionality")]
#pragma warning restore S1133
        public static Task<TemporaryTableEntity> AddIfNotExistsAsync<TEntity>(TableClient client, TEntity entity, ILogger logger) where TEntity : class, ITableEntity
        {
            return UpsertEntityAsync(client, entity, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing entity in an Azure Table.
        /// </summary>
        /// <remarks>
        ///     Based on the entity's <see cref="ITableEntity.PartitionKey" /> and <see cref="ITableEntity.RowKey" />,
        ///     an existing entity will be replaced or a new entity will be created.
        /// </remarks>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="accountName">The name of the Azure Storage account where the table for the entity is located.</param>
        /// <param name="tableName">The name of the Azure Table where the table entity should be added.</param>
        /// <param name="entity">The entity to add to the Azure Table.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or the <paramref name="tableName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="RequestFailedException">Thrown when the interaction with Azure Table Storage failed.</exception>
        public static Task<TemporaryTableEntity> UpsertEntityAsync<TEntity>(
            string accountName,
            string tableName,
            TEntity entity,
            ILogger logger)
            where TEntity : class, ITableEntity
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            var tableEndpoint = new Uri($"https://{accountName}.table.core.windows.net");
            var tableClient = new TableClient(tableEndpoint, tableName, new DefaultAzureCredential());

            return UpsertEntityAsync(tableClient, entity, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing entity in an Azure Table.
        /// </summary>
        /// <remarks>
        ///     Based on the entity's <see cref="ITableEntity.PartitionKey" /> and <see cref="ITableEntity.RowKey" />,
        ///     an existing entity will be replaced or a new entity will be created.
        /// </remarks>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="client">The client to interact with the Azure Table resource.</param>
        /// <param name="entity">The entity to add to the Azure Table.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> or the <paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="RequestFailedException">Thrown when the interaction with Azure Table Storage failed.</exception>
        public static async Task<TemporaryTableEntity> UpsertEntityAsync<TEntity>(TableClient client, TEntity entity, ILogger logger) where TEntity : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(entity);
            logger ??= NullLogger.Instance;

            Type entityType = typeof(TEntity);

            NullableResponse<TEntity> entityExists =
                await client.GetEntityIfExistsAsync<TEntity>(entity.PartitionKey, entity.RowKey).ConfigureAwait(false);

            if (entityExists.HasValue)
            {
                ITableEntity originalEntity = entityExists.Value;

                logger.LogSetupReplaceEntity(entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
                using Response response = await client.UpsertEntityAsync(entity, TableUpdateMode.Replace).ConfigureAwait(false);

                if (response.IsError)
                {
                    throw new RequestFailedException(
                        $"[Test:Setup] Failed to replace an existing Azure Table entity '{typeof(TEntity).Name}' (rowKey: '{entity.RowKey}', partitionKey: '{entity.PartitionKey}') in table '{client.AccountName}/{client.Name}' " +
                        $"since the upsert operation responded with failure: {response.Status} {(HttpStatusCode) response.Status}",
                        new RequestFailedException(response));
                }

                return new TemporaryTableEntity(client, entityType, createdByUs: false, originalEntity, logger);
            }

            else
            {
                logger.LogSetupAddNewEntity(entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
                using Response response = await client.AddEntityAsync(entity).ConfigureAwait(false);

                if (response.IsError)
                {
                    throw new RequestFailedException(
                        $"[Test:Setup] Failed to add a new Azure Table entity '{typeof(TEntity).Name}' (rowKey: '{entity.RowKey}', partitionKey: '{entity.PartitionKey}') in table '{client.AccountName}/{client.Name}' " +
                        $"since the add operation responded with failure: {response.Status} {(HttpStatusCode) response.Status}",
                        new RequestFailedException(response));
                }

                return new TemporaryTableEntity(client, entityType, createdByUs: true, entity, logger);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_createdByUs)
            {
                _logger.LogTeardownDeleteEntity(_entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.AccountName, _tableClient.Name);
                using Response response = await _tableClient.DeleteEntityAsync(_entity).ConfigureAwait(false);

                if (response.IsError && response.Status != NotFound)
                {
                    throw new RequestFailedException(
                        $"[Test:Teardown] Failed to delete a Azure Table entity '{_entityType.Name}' (rowKey: '{_entity.RowKey}', partitionKey: '{_entity.PartitionKey}') in table '{_tableClient.AccountName}/{_tableClient.Name}' " +
                        $"since the delete operation responded with failure: {response.Status} {(HttpStatusCode) response.Status}",
                        new RequestFailedException(response));
                }
            }
            else
            {
                _logger.LogTeardownRevertEntity(_entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.AccountName, _tableClient.Name);
                using Response response = await _tableClient.UpsertEntityAsync(_entity, TableUpdateMode.Replace).ConfigureAwait(false);

                if (response.IsError)
                {
                    throw new RequestFailedException(
                        $"[Test:Teardown] Failed to revert a Azure Table entity '{_entityType.Name}' (rowKey: '{_entity.RowKey}', partitionKey: '{_entity.PartitionKey}') in table '{_tableClient.AccountName}/{_tableClient.Name}' " +
                        $"since the upsert operation responded with failure: {response.Status} {(HttpStatusCode) response.Status}",
                        new RequestFailedException(response));
                }
            }

            GC.SuppressFinalize(this);
        }
    }

    internal static partial class TempTableEntityILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Add new Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'")]
        internal static partial void LogSetupAddNewEntity(this ILogger logger, string entityType, string rowKey, string partitionKey, string accountName, string tableName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Replace already existing Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'")]
        internal static partial void LogSetupReplaceEntity(this ILogger logger, string entityType, string rowKey, string partitionKey, string accountName, string tableName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'")]
        internal static partial void LogTeardownDeleteEntity(this ILogger logger, string entityType, string rowKey, string partitionKey, string accountName, string tableName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert replaced Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'")]
        internal static partial void LogTeardownRevertEntity(this ILogger logger, string entityType, string rowKey, string partitionKey, string accountName, string tableName);
    }
}
