using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Table that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryTable : IAsyncDisposable
    {
        private readonly TableClient _client;
        private readonly bool _createdByUs;
        private readonly Collection<TemporaryTableEntity> _entities = new();
        private readonly ILogger _logger;

        private TemporaryTable(TableClient client, bool createdByUs, ILogger logger)
        {
            _client = client;
            _createdByUs = createdByUs;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="tableEndpoint">
        ///     A <see cref="TableClient.Uri" /> referencing the table service account.
        ///     This is likely to be similar to "https://{account_name}.table.core.windows.net/?{sas_token}" or
        ///     "https://{account_name}.table.cosmos.azure.com?{sas_token}".
        /// </param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tableEndpoint"/> or <paramref name="tableName"/> is blank.</exception>
        public static async Task<TemporaryTable> CreateIfNotExistsAsync(Uri tableEndpoint, string tableName, ILogger logger)
        {
            if (tableEndpoint is null)
            {
                throw new ArgumentNullException(nameof(tableEndpoint));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Table name to create a temporary Azure Table test fixture," +
                    " used in container URI: 'https://{account_name}.table.core.windows.net/{container_name}'", nameof(tableName));
            }

            bool createdByUs = await EnsureTableCreatedAsync(tableEndpoint, tableName, logger);

            var tableClient = new TableClient(tableEndpoint, tableName, new DefaultAzureCredential());
            return new TemporaryTable(tableClient, createdByUs, logger);
        }

        private static async Task<bool> EnsureTableCreatedAsync(Uri tableEndpoint, string tableName, ILogger logger)
        {
            var createdByUs = false;

            var exists = false;
            var serviceClient = new TableServiceClient(tableEndpoint, new DefaultAzureCredential());
            await foreach (TableItem _ in serviceClient.QueryAsync(t => t.Name == tableName))
            {
                exists = true;
            }

            if (exists)
            {
                logger.LogDebug("Use already existing Azure Table '{TableName}' in account '{AccountName}'", tableName, serviceClient.AccountName);
            }
            else
            {
                logger.LogDebug("Creating Azure Table '{TableName}' in account '{AccountName}'", tableName, serviceClient.AccountName);
                await serviceClient.CreateTableIfNotExistsAsync(tableName);
                createdByUs = true;
            }

            return createdByUs;
        }


        /// <summary>
        /// Adds a temporary <paramref name="entity"/> to the Azure Table.
        /// </summary>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="entity">The entity to temporary add to the table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is <c>null</c>.</exception>
        public async Task AddEntityAsync<TEntity>(TEntity entity) where TEntity : class, ITableEntity
        {
            _entities.Add(await TemporaryTableEntity.AddIfNotExistsAsync(_client, entity, _logger));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (_createdByUs)
            {
                _logger.LogTrace("Deleting Azure Table '{TableName}' in account '{AccountName}'", _client.Name, _client.AccountName);
                await _client.DeleteAsync(); 
            }
            else
            {
                disposables.AddRange(_entities);
            }

            GC.SuppressFinalize(this);
        }
    }
}
