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
        public static async Task<TemporaryTableEntity> AddIfNotExistsAsync<TEntity>(
            string accountName,
            string tableName,
            TEntity entity,
            ILogger logger)
            where TEntity : class, ITableEntity
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Storage account name to create a temporary Azure Table entity test fixture," +
                    " used in container URI: 'https://{account_name}.table.core.windows.net'", nameof(accountName));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Table nme to create a temporary Azure Table entity test fixture", nameof(tableName));
            }

            var tableEndpoint = new Uri($"https://{accountName}.table.core.windows.net");
            var tableClient = new TableClient(tableEndpoint, tableName, new DefaultAzureCredential());

            return await AddIfNotExistsAsync<TEntity>(tableClient, entity, logger);
        }

        /// <summary>
        /// Creates a temporary entity in an Azure Table.
        /// </summary>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="client">The client to interact with the Azure Table resource.</param>
        /// <param name="entity">The entity to add to the Azure Table.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> or the <paramref name="entity"/> is <c>null</c>.</exception>
        public static async Task<TemporaryTableEntity> AddIfNotExistsAsync<TEntity>(TableClient client, TEntity entity, ILogger logger) where TEntity : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(entity);
            logger ??= NullLogger.Instance;

            Type entityType = typeof(TEntity);

            NullableResponse<TEntity> entityExists =
                await client.GetEntityIfExistsAsync<TEntity>(entity.PartitionKey, entity.RowKey);

            if (entityExists.HasValue)
            {
                ITableEntity originalEntity = entityExists.Value;
                
                logger.LogDebug("[Test:Setup] Replace already existing Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'", entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
                using Response response = await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);

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
                logger.LogDebug("[Test:Setup] Add new Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'", entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
                using Response response = await client.AddEntityAsync(entity);

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
                _logger.LogDebug("[Test:Teardown] Delete Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", _entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.AccountName, _tableClient.Name);
                using Response response = await _tableClient.DeleteEntityAsync(_entity);

                if (response.IsError)
                {
                    throw new RequestFailedException(
                        $"[Test:Teardown] Failed to delete a Azure Table entity '{_entityType.Name}' (rowKey: '{_entity.RowKey}', partitionKey: '{_entity.PartitionKey}') in table '{_tableClient.AccountName}/{_tableClient.Name}' " +
                        $"since the delete operation responded with failure: {response.Status} {(HttpStatusCode) response.Status}",
                        new RequestFailedException(response));
                }
            }
            else
            {
                _logger.LogDebug("[Test:Teardown] Revert replaced Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", _entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.AccountName, _tableClient.Name);
                using Response response = await _tableClient.UpsertEntityAsync(_entity, TableUpdateMode.Replace);

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
}
