using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryTable"/> is created.
    /// </summary>
    internal enum OnSetupTable { LeaveExisted = 0, CleanIfExisted = 1, CleanIfMatched = 2 }

    /// <summary>
    /// Represents the available options when the <see cref="TemporaryTable"/> is deleted.
    /// </summary>
    internal enum OnTeardownTable { CleanIfCreated = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents a filter to match against a stored Azure Table entity in the <see cref="TemporaryTable"/> upon setup or teardown.
    /// </summary>
    public class TableEntityFilter
    {
        private readonly Func<TableEntity, bool> _filter;

        private TableEntityFilter(Func<TableEntity, bool> filter)
        {
            _filter = filter;
        }

        /// <summary>
        /// Creates a <see cref="TableEntityFilter"/> to match an Azure Table entity by its unique row key.
        /// </summary>
        /// <param name="rowKey">The expected unique identifier of the entity.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rowKey"/> is blank.</exception>
        public static TableEntityFilter RowKeyEqual(string rowKey)
        {
            return RowKeyEqual(rowKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates a <see cref="TableEntityFilter"/> to match an Azure Table entity by its unique row key.
        /// </summary>
        /// <param name="rowKey">The expected unique identifier of the entity.</param>
        /// <param name="comparisonType">The value that specifies how the strings will be compared.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rowKey"/> is blank.</exception>
        public static TableEntityFilter RowKeyEqual(string rowKey, StringComparison comparisonType)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Requires a non-blank Azure Table entity row key to match against stored entities");
            }

            return EntityEqual(e => rowKey.Equals(e.RowKey, comparisonType));
        }

        /// <summary>
        /// Creates a <see cref="TableEntityFilter"/> to match an Azure Table entity by its partition key.
        /// </summary>
        /// <param name="partitionKey">The expected partition key of the entity.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="partitionKey"/> is blank.</exception>
        public static TableEntityFilter PartitionKeyEqual(string partitionKey)
        {
            return PartitionKeyEqual(partitionKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates a <see cref="TableEntityFilter"/> to match an Azure Table entity by its partition key.
        /// </summary>
        /// <param name="partitionKey">The expected partition key of the entity.</param>
        /// <param name="comparisonType">The value that specifies how the strings will be compared.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="partitionKey"/> is blank.</exception>
        public static TableEntityFilter PartitionKeyEqual(string partitionKey, StringComparison comparisonType)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException("Requires a non-blank Azure Table entity partition key to match against stored entities");
            }

            return EntityEqual(e => partitionKey.Equals(e.PartitionKey, comparisonType));
        }

        /// <summary>
        /// Creates a <see cref="TableEntityFilter"/> to match an Azure Table entity by its unique row key.
        /// </summary>
        /// <param name="filter">The custom filter to match against an entity.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filter"/> is <c>null</c>.</exception>
        public static TableEntityFilter EntityEqual(Func<TableEntity, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return new TableEntityFilter(filter);
        }

        /// <summary>
        /// Match the current Azure Table entity with the user configured filter.
        /// </summary>
        internal bool IsMatch(TableEntity entity)
        {
            return _filter(entity);
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryTable"/>.
    /// </summary>
    public class OnSetupTemporaryTableOptions
    {
        private readonly List<TableEntityFilter> _filters = new();

        /// <summary>
        /// Gets the configurable setup option on what to do with existing entities in the Azure Table upon the test fixture creation.
        /// </summary>
        internal OnSetupTable Entities { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryTable"/> to leave all Azure Table entities untouched
        /// that already existed upon the test fixture creation, when there was already an Azure Table available.
        /// </summary>
        public OnSetupTemporaryTableOptions LeaveAllEntities()
        {
            Entities = OnSetupTable.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTable"/> to delete all the already existing Azure Table entities upon the test fixture creation.
        /// </summary>
        public OnSetupTemporaryTableOptions CleanAllEntities()
        {
            Entities = OnSetupTable.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTable"/> to delete the Azure Table entities
        /// upon the test fixture creation that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will be aggregated together in an OR expression.
        /// </remarks>
        /// <param name="filters">The filters to match entities that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more <paramref name="filters"/> is <c>null</c>.</exception>
        public OnSetupTemporaryTableOptions CleanMatchingEntities(params TableEntityFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Entities = OnSetupTable.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current Azure Table entity.
        /// </summary>
        internal bool IsMatch(TableEntity entity)
        {
            return _filters.Exists(filter => filter.IsMatch(entity));
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryTable"/>.
    /// </summary>
    public class OnTeardownTemporaryTableOptions
    {
        private readonly List<TableEntityFilter> _filters = new();

        /// <summary>
        /// Gets the configurable setup option on what to do with existing entities in the Azure Table upon the test fixture deletion.
        /// </summary>
        internal OnTeardownTable Entities { get; private set; }

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryTable"/> to only delete the Azure Table entities upon disposal
        /// if the document was inserted by the test fixture (using <see cref="TemporaryTable.AddEntityAsync{TEntity}"/>).
        /// </summary>
        public OnTeardownTemporaryTableOptions CleanCreatedEntities()
        {
            Entities = OnTeardownTable.CleanIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTable"/> to delete all the Azure Table entities upon disposal - even if the test fixture didn't add them.
        /// </summary>
        public OnTeardownTemporaryTableOptions CleanAllEntities()
        {
            Entities = OnTeardownTable.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryTable"/> to delete the Azure Table entities
        /// upon disposal that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     The matching of documents only happens on entities that were created outside the scope of the test fixture.
        ///     All items created by the test fixture will be deleted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        /// </remarks>
        /// <param name="filters">The filters  to match entities that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains <c>null</c>.</exception>
        public OnTeardownTemporaryTableOptions CleanMatchingEntities(params TableEntityFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Entities = OnTeardownTable.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current Azure Table entity.
        /// </summary>
        internal bool IsMatch(TableEntity entity)
        {
            return _filters.Exists(filter => filter.IsMatch(entity));
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryTable"/>.
    /// </summary>
    public class TemporaryTableOptions
    {
        /// <summary>
        /// Gets the additional options to manipulate the creation of the <see cref="TemporaryTable"/>.
        /// </summary>
        public OnSetupTemporaryTableOptions OnSetup { get; } = new OnSetupTemporaryTableOptions().LeaveAllEntities();

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryTable"/>.
        /// </summary>
        public OnTeardownTemporaryTableOptions OnTeardown { get; } = new OnTeardownTemporaryTableOptions().CleanCreatedEntities();
    }

    /// <summary>
    /// Represents a temporary Azure Table that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryTable : IAsyncDisposable
    {
        private readonly bool _createdByUs;
        private readonly Collection<TemporaryTableEntity> _entities = new();
        private readonly TemporaryTableOptions _options;
        private readonly ILogger _logger;

        private TemporaryTable(TableClient client, bool createdByUs, TemporaryTableOptions options, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);

            _createdByUs = createdByUs;
            _options = options;
            _logger = logger ?? NullLogger.Instance;
            
            Client = client;
        }

        /// <summary>
        /// Gets the name of the currently remotely available Azure Table being set up by the test fixture.
        /// </summary>
        public string Name => Client.Name;

        /// <summary>
        /// Gets the client to interact with the Azure Table currently being set up by the test fixture.
        /// </summary>
        public TableClient Client { get; }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryTable"/>.
        /// </summary>
        public OnTeardownTemporaryTableOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="accountName">The name of the Azure Storage account where the table should be added.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="tableName"/> is blank.</exception>
        public static async Task<TemporaryTable> CreateIfNotExistsAsync(string accountName, string tableName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(accountName, tableName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="accountName">The name of the Azure Storage account where the table should be added.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="tableName"/> is blank.</exception>
        public static async Task<TemporaryTable> CreateIfNotExistsAsync(
            string accountName,
            string tableName,
            ILogger logger,
            Action<TemporaryTableOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Storage account name to create a temporary Azure Table test fixture," +
                    " used in container URI: 'https://{account_name}.table.core.windows.net'", nameof(accountName));
            }

            var tableEndpoint = new Uri($"https://{accountName}.table.core.windows.net");
            var serviceClient = new TableServiceClient(tableEndpoint, new DefaultAzureCredential());

            return await CreateIfNotExistsAsync(serviceClient, tableName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="serviceClient">The client to interact with the Azure Table storage resource as a whole.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tableName"/> is blank.</exception>
        public static async Task<TemporaryTable> CreateIfNotExistsAsync(TableServiceClient serviceClient, string tableName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(serviceClient, tableName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="serviceClient">The client to interact with the Azure Table storage resource as a whole.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tableName"/> is blank.</exception>
        public static async Task<TemporaryTable> CreateIfNotExistsAsync(
            TableServiceClient serviceClient,
            string tableName,
            ILogger logger,
            Action<TemporaryTableOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(serviceClient);
            ArgumentNullException.ThrowIfNull(tableName);

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Table nme to create a temporary Azure Table test fixture", nameof(tableName));
            }

            logger ??= NullLogger.Instance;
            var options = new TemporaryTableOptions();
            configureOptions?.Invoke(options);

            bool createdByUs = await EnsureTableCreatedAsync(serviceClient, tableName, logger);
            TableClient tableClient = serviceClient.GetTableClient(tableName);

            await CleanTableUponSetupAsync(tableClient, options, logger);
            return new TemporaryTable(tableClient, createdByUs, options, logger);
        }

        private static async Task<bool> EnsureTableCreatedAsync(TableServiceClient serviceClient, string tableName, ILogger logger)
        {
            var createdByUs = false;

            var exists = false;
            await foreach (TableItem _ in serviceClient.QueryAsync(t => t.Name == tableName))
            {
                exists = true;
            }

            if (exists)
            {
                logger.LogDebug("[Test:Setup] Use already existing Azure Table '{TableName}' in account '{AccountName}'", tableName, serviceClient.AccountName);
            }
            else
            {
                logger.LogDebug("[Test:Setup] Create new Azure Table '{TableName}' in account '{AccountName}'", tableName, serviceClient.AccountName);
                await serviceClient.CreateTableIfNotExistsAsync(tableName);
                createdByUs = true;
            }

            return createdByUs;
        }

        private static async Task CleanTableUponSetupAsync(TableClient tableClient, TemporaryTableOptions options, ILogger logger)
        {
            if (options.OnSetup.Entities is OnSetupTable.LeaveExisted)
            {
                return;
            }

            if (options.OnSetup.Entities is OnSetupTable.CleanIfExisted)
            {
                await foreach (TableEntity item in tableClient.QueryAsync<TableEntity>(_ => true))
                {
                    logger.LogTrace("[Test:Setup] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", item.RowKey, item.PartitionKey, tableClient.AccountName, tableClient.Name);
                    using Response response = await tableClient.DeleteEntityAsync(item);

                    if (response.IsError)
                    {
                        throw new RequestFailedException(
                            $"[Test:Teardown] Failed to delete Azure Table entity (rowKey: '{item.RowKey}', partitionKey: '{item.PartitionKey}') from table {tableClient.AccountName}/{tableClient.Name}' " +
                            $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}", 
                            new RequestFailedException(response));
                    }
                }
            }
            else if (options.OnSetup.Entities is OnSetupTable.CleanIfMatched)
            {
                await foreach (TableEntity item in tableClient.QueryAsync<TableEntity>(_ => true))
                {
                    if (options.OnSetup.IsMatch(item))
                    {
                        logger.LogTrace("[Test:Setup] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", item.RowKey, item.PartitionKey, tableClient.AccountName, tableClient.Name);
                        using Response response = await tableClient.DeleteEntityAsync(item);

                        if (response.IsError)
                        {
                            throw new RequestFailedException(
                                $"[Test:Setup] Failed to delete Azure Table entity (rowKey: '{item.RowKey}', partitionKey: '{item.PartitionKey}') from table {tableClient.AccountName}/{tableClient.Name}' " +
                                $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}", 
                                new RequestFailedException(response));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a temporary <paramref name="entity"/> to the Azure Table.
        /// </summary>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="entity">The entity to temporary add to the table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is <c>null</c>.</exception>
        public async Task AddEntityAsync<TEntity>(TEntity entity) where TEntity : class, ITableEntity
        {
            ArgumentNullException.ThrowIfNull(entity);
            _entities.Add(await TemporaryTableEntity.AddIfNotExistsAsync(Client, entity, _logger));
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
                _logger.LogDebug("[Test:Teardown] Delete Azure Table '{TableName}' in account '{AccountName}'", Client.Name, Client.AccountName);
                using Response response = await Client.DeleteAsync(); 

                if (response.IsError)
                {
                    throw new RequestFailedException(
                        $"[Test:Teardown] Failed to delete Azure Table {Client.AccountName}/{Client.Name}' " +
                        $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}", 
                        new RequestFailedException(response));
                }
            }
            else
            {
                disposables.AddRange(_entities);
                await CleanTableUponTeardownAsync(disposables);
            }

            GC.SuppressFinalize(this);
        }

        private async Task CleanTableUponTeardownAsync(DisposableCollection disposables)
        {
            if (_options.OnTeardown.Entities is OnTeardownTable.CleanIfCreated)
            {
                return;
            }

            if (_options.OnTeardown.Entities is OnTeardownTable.CleanAll)
            {
                await foreach (TableEntity item in Client.QueryAsync<TableEntity>(_ => true))
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTrace("[Test:Teardown] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", item.RowKey, item.PartitionKey, Client.AccountName, Client.Name);
                        using Response response = await Client.DeleteEntityAsync(item);

                        if (response.IsError)
                        {
                            throw new RequestFailedException(
                                $"[Test:Teardown] Failed to delete Azure Table entity (rowKey: '{item.RowKey}', partitionKey: '{item.PartitionKey}') from table {Client.AccountName}/{Client.Name}' " +
                                $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}", 
                                new RequestFailedException(response));
                        }
                    }));
                } 
            }
            else if (_options.OnTeardown.Entities is OnTeardownTable.CleanIfMatched)
            {
                await foreach (TableEntity item in Client.QueryAsync<TableEntity>(_ => true))
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        if (_options.OnTeardown.IsMatch(item))
                        {
                            _logger.LogTrace("[Test:Teardown] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'", item.RowKey, item.PartitionKey, Client.AccountName, Client.Name);
                            using Response response = await Client.DeleteEntityAsync(item);

                            if (response.IsError)
                            {
                                throw new RequestFailedException(
                                    $"[Test:Teardown] Failed to delete Azure Table entity (rowKey: '{item.RowKey}', partitionKey: '{item.PartitionKey}') from table {Client.AccountName}/{Client.Name}' " +
                                    $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}", 
                                    new RequestFailedException(response));
                            }
                        }
                    }));
                } 
            }
        }
    }
}
