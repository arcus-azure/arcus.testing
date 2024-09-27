using System;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
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
            _tableClient = tableClient;
            _entityType = entityType;
            _createdByUs = createdByUs;
            _entity = originalEntity;
            _logger = logger;
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
                
                logger.LogTrace("Replace existing Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'", entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
                await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);

                return new TemporaryTableEntity(client, entityType, createdByUs: false, originalEntity, logger);
            }

            logger.LogTrace("Add new Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') in table '{AccountName}/{TableName}'", entityType.Name, entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
            await client.AddEntityAsync(entity);

            return new TemporaryTableEntity(client, entityType, createdByUs: true, entity, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_createdByUs)
            {
                _logger.LogTrace("Deleting Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{TableName}' in account '{AccountName}'", _entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.Name, _tableClient.AccountName);
                await _tableClient.DeleteEntityAsync(_entity);
            }
            else
            {
                _logger.LogTrace("Revert replaced Azure Table entity '{EntityType}' (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{TableName}' in account '{AccountName}'", _entityType.Name, _entity.RowKey, _entity.PartitionKey, _tableClient.Name, _tableClient.AccountName);
                await _tableClient.UpsertEntityAsync(_entity, TableUpdateMode.Replace);
            }

            GC.SuppressFinalize(this);
        }
    }
}
