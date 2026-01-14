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
    internal enum OnTeardownTable { CleanIfUpserted = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryTable"/>.
    /// </summary>
    public class OnSetupTemporaryTableOptions
    {
        private readonly List<Func<TableEntity, bool>> _filters = [];

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
        public OnSetupTemporaryTableOptions CleanMatchingEntities(params Func<TableEntity, bool>[] filters)
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
            return _filters.Exists(filter => filter(entity));
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryTable"/>.
    /// </summary>
    public class OnTeardownTemporaryTableOptions
    {
        private readonly List<Func<TableEntity, bool>> _filters = [];

        /// <summary>
        /// Gets the configurable setup option on what to do with existing entities in the Azure Table upon the test fixture deletion.
        /// </summary>
        internal OnTeardownTable Entities { get; private set; }

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryTable"/> to only delete the Azure Table entities upon disposal
        /// if the document was inserted by the test fixture (using <see cref="TemporaryTable.UpsertEntityAsync{TEntity}"/>).
        /// </summary>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(CleanUpsertedEntities) + " instead that provides exactly the same functionality")]
        public OnTeardownTemporaryTableOptions CleanCreatedEntities()
        {
            return CleanUpsertedEntities();
        }

        /// <summary>
        /// (default for cleaning documents) Configures the <see cref="TemporaryTable"/> to only delete or revert the Azure Table entities upon disposal
        /// if the document was upserted by the test fixture (using <see cref="TemporaryTable.UpsertEntityAsync{TEntity}"/>).
        /// </summary>
        public OnTeardownTemporaryTableOptions CleanUpsertedEntities()
        {
            Entities = OnTeardownTable.CleanIfUpserted;
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
        ///     All items created by the test fixture will be deleted or reverted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        /// </remarks>
        /// <param name="filters">The filters  to match entities that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains <c>null</c>.</exception>
        public OnTeardownTemporaryTableOptions CleanMatchingEntities(params Func<TableEntity, bool>[] filters)
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
            return _filters.Exists(filter => filter(entity));
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
        public OnTeardownTemporaryTableOptions OnTeardown { get; } = new OnTeardownTemporaryTableOptions().CleanUpsertedEntities();
    }

    /// <summary>
    /// Represents a temporary Azure Table that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryTable : IAsyncDisposable
    {
        private const int NotFound = 404;

        private readonly TableClient _client;
        private readonly bool _createdByUs;

        private readonly Collection<TemporaryTableEntity> _entities = [];
        private readonly TemporaryTableOptions _options;
        private readonly DisposableCollection _disposables;
        private readonly ILogger _logger;

        private TemporaryTable(TableClient client, bool createdByUs, TemporaryTableOptions options, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);

            _client = client;
            _createdByUs = createdByUs;

            _options = options;
            _logger = logger ?? NullLogger.Instance;
            _disposables = new DisposableCollection(_logger);
        }

        /// <summary>
        /// Gets the name of the currently remotely available Azure Table being set up by the test fixture.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public string Name => Client.Name;

        /// <summary>
        /// Gets the client to interact with the Azure Table currently being set up by the test fixture.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public TableClient Client
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
                return _client;
            }
        }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryTable"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public OnTeardownTemporaryTableOptions OnTeardown
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
                return _options.OnTeardown;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="accountName">The name of the Azure Storage account where the table should be added.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="tableName"/> is blank.</exception>
        public static Task<TemporaryTable> CreateIfNotExistsAsync(string accountName, string tableName, ILogger logger)
        {
            return CreateIfNotExistsAsync(accountName, tableName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="accountName">The name of the Azure Storage account where the table should be added.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="tableName"/> is blank.</exception>
        public static Task<TemporaryTable> CreateIfNotExistsAsync(
            string accountName,
            string tableName,
            ILogger logger,
            Action<TemporaryTableOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

            var tableEndpoint = new Uri($"https://{accountName}.table.core.windows.net");
            var serviceClient = new TableServiceClient(tableEndpoint, new DefaultAzureCredential());

            return CreateIfNotExistsAsync(serviceClient, tableName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="serviceClient">The client to interact with the Azure Table Storage resource as a whole.</param>
        /// <param name="tableName">The name of the Azure Table to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tableName"/> is blank.</exception>
        public static Task<TemporaryTable> CreateIfNotExistsAsync(TableServiceClient serviceClient, string tableName, ILogger logger)
        {
            return CreateIfNotExistsAsync(serviceClient, tableName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTable"/> which creates a new Azure Table container if it doesn't exist yet.
        /// </summary>
        /// <param name="serviceClient">The client to interact with the Azure Table Storage resource as a whole.</param>
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
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

            logger ??= NullLogger.Instance;
            var options = new TemporaryTableOptions();
            configureOptions?.Invoke(options);

            bool createdByUs = await EnsureTableCreatedAsync(serviceClient, tableName, logger).ConfigureAwait(false);
            TableClient tableClient = serviceClient.GetTableClient(tableName);

            await CleanTableUponSetupAsync(tableClient, options, logger).ConfigureAwait(false);
            return new TemporaryTable(tableClient, createdByUs, options, logger);
        }

        private static async Task<bool> EnsureTableCreatedAsync(TableServiceClient serviceClient, string tableName, ILogger logger)
        {
            var createdByUs = false;

            var exists = false;
            await foreach (TableItem _ in serviceClient.QueryAsync(t => t.Name == tableName).ConfigureAwait(false))
            {
                exists = true;
            }

            if (exists)
            {
                logger.LogSetupUseExistingTable(tableName, serviceClient.AccountName);
            }
            else
            {
                logger.LogSetupCreateNewTable(tableName, serviceClient.AccountName);
                await serviceClient.CreateTableIfNotExistsAsync(tableName).ConfigureAwait(false);

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
                await foreach (TableEntity item in tableClient.QueryAsync<TableEntity>(_ => true).ConfigureAwait(false))
                {
                    await DeleteEntityOnSetupAsync(tableClient, item, logger).ConfigureAwait(false);
                }
            }
            else if (options.OnSetup.Entities is OnSetupTable.CleanIfMatched)
            {
#pragma warning disable S3267 // Sonar recommends LINQ on loops, but Microsoft has no Async LINQ built-in, besides the additional/outdated `System.Linq.Async` package.
                await foreach (TableEntity item in tableClient.QueryAsync<TableEntity>(_ => true).ConfigureAwait(false))
#pragma warning restore
                {
                    if (options.OnSetup.IsMatch(item))
                    {
                        await DeleteEntityOnSetupAsync(tableClient, item, logger).ConfigureAwait(false);
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
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertEntityAsync) + " instead that provides exactly the same functionality")]
        public Task AddEntityAsync<TEntity>(TEntity entity) where TEntity : class, ITableEntity
        {
            return UpsertEntityAsync(entity);
        }

        /// <summary>
        /// Adds a new or replaces an existing item in the Azure Table (a.k.a. UPSERT).
        /// </summary>
        /// <remarks>
        ///     ⚡ Any entities upserted via this call will always be deleted (if new) or reverted (if existing) when the <see cref="TemporaryTable"/> is disposed.
        ///     Existing entities are found based on their <see cref="ITableEntity.PartitionKey" /> and <see cref="ITableEntity.RowKey" />.
        /// </remarks>
        /// <typeparam name="TEntity">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity" />.</typeparam>
        /// <param name="entity">The entity to temporary add to the table.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is <c>null</c>.</exception>
        public async Task UpsertEntityAsync<TEntity>(TEntity entity) where TEntity : class, ITableEntity
        {
            ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
            ArgumentNullException.ThrowIfNull(entity);

            _entities.Add(await TemporaryTableEntity.UpsertEntityAsync(_client, entity, _logger).ConfigureAwait(false));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposables.IsDisposed)
            {
                return;
            }

            await using (_disposables.ConfigureAwait(false))
            {
                if (_createdByUs)
                {
                    _logger.LogTeardownDeleteTable(_client.Name, _client.AccountName);
                    using Response response = await _client.DeleteAsync().ConfigureAwait(false);

                    if (response.IsError && response.Status != NotFound)
                    {
                        throw new RequestFailedException(
                            $"[Test:Teardown] Failed to delete Azure Table {_client.AccountName}/{_client.Name}' " +
                            $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}",
                            new RequestFailedException(response));
                    }
                }
                else
                {
                    _disposables.AddRange(_entities);
                    await CleanTableUponTeardownAsync(_disposables).ConfigureAwait(false);
                }

                GC.SuppressFinalize(this);
            }
        }

        private async Task CleanTableUponTeardownAsync(DisposableCollection disposables)
        {
            if (_options.OnTeardown.Entities is OnTeardownTable.CleanIfUpserted)
            {
                return;
            }

            if (_options.OnTeardown.Entities is OnTeardownTable.CleanAll)
            {
                await foreach (TableEntity item in _client.QueryAsync<TableEntity>(_ => true).ConfigureAwait(false))
                {
                    disposables.Add(AsyncDisposable.Create(() => DeleteEntityOnTeardownAsync(item)));
                }
            }
            else if (_options.OnTeardown.Entities is OnTeardownTable.CleanIfMatched)
            {
#pragma warning disable S3267 // Sonar recommends LINQ on loops, but Microsoft has no Async LINQ built-in, besides the additional/outdated `System.Linq.Async` package.
                await foreach (TableEntity item in _client.QueryAsync<TableEntity>(_ => true).ConfigureAwait(false))
#pragma warning restore S3267
                {
                    if (_options.OnTeardown.IsMatch(item))
                    {
                        disposables.Add(AsyncDisposable.Create(() => DeleteEntityOnTeardownAsync(item)));
                    }
                }
            }
        }

        private static Task DeleteEntityOnSetupAsync(TableClient client, TableEntity entity, ILogger logger)
        {
            logger.LogSetupDeleteEntity(entity.RowKey, entity.PartitionKey, client.AccountName, client.Name);
            return DeleteEntityAsync(client, entity, "[Test:Setup]");
        }

        private Task DeleteEntityOnTeardownAsync(TableEntity entity)
        {
            _logger.LogTeardownDeleteEntity(entity.RowKey, entity.PartitionKey, _client.AccountName, _client.Name);
            return DeleteEntityAsync(_client, entity, "[Test:Teardown]");
        }

        private static async Task DeleteEntityAsync(TableClient client, TableEntity entity, string testOperation)
        {
            using Response response = await client.DeleteEntityAsync(entity).ConfigureAwait(false);

            if (response.IsError && response.Status != NotFound)
            {
                throw new RequestFailedException(
                    $"{testOperation} Failed to delete Azure Table entity (rowKey: '{entity.RowKey}', partitionKey: '{entity.PartitionKey}') from table {client.AccountName}/{client.Name}' " +
                    $"since the delete operation responded with a failure: {response.Status} {(HttpStatusCode) response.Status}",
                    new RequestFailedException(response));
            }
        }
    }

    internal static partial class TempTableILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Create new Azure Table '{TableName}' in account '{AccountName}'")]
        internal static partial void LogSetupCreateNewTable(this ILogger logger, string tableName, string accountName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Use already existing Azure Table '{TableName}' in account '{AccountName}'")]
        internal static partial void LogSetupUseExistingTable(this ILogger logger, string tableName, string accountName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'")]
        internal static partial void LogSetupDeleteEntity(this ILogger logger, string rowKey, string partitionKey, string accountName, string tableName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Table entity (rowKey: '{RowKey}', partitionKey: '{PartitionKey}') from table '{AccountName}/{TableName}'")]
        internal static partial void LogTeardownDeleteEntity(this ILogger logger, string rowKey, string partitionKey, string accountName, string tableName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Table '{TableName}' in account '{AccountName}'")]
        internal static partial void LogTeardownDeleteTable(this ILogger logger, string tableName, string accountName);
    }
}
