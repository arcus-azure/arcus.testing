using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    /// <summary>
    /// Represents a test context instance that provides meaningful interaction points with Azure Table storage.
    /// </summary>
    public class TableStorageTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly Collection<string> _tableNames = new();
        private readonly TableServiceClient _serviceClient;
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private TableStorageTestContext(
            TemporaryManagedIdentityConnection connection,
            TableServiceClient serviceClient,
            ILogger logger)
        {
            _connection = connection;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new <see cref="TableStorageTestContext"/> that interacts with Azure Table Storage.
        /// </summary>
        public static Task<TableStorageTestContext> GivenAsync(TestConfig configuration, ILogger logger)
        {
            var connection = TemporaryManagedIdentityConnection.Create(configuration.GetServicePrincipal());
            
            StorageAccount storageAccount = configuration.GetStorageAccount();
            var serviceClient = new TableServiceClient(
                new Uri($"https://{storageAccount.Name}.table.core.windows.net"),
                new DefaultAzureCredential());

            return Task.FromResult(new TableStorageTestContext(connection, serviceClient, logger));
        }

        /// <summary>
        /// Provides a table that is currently not available remotely.
        /// </summary>
        public TableClient WhenTableUnavailable()
        {
            string tableName = $"table{Guid.NewGuid()}".Replace("-", "");
            _tableNames.Add(tableName);

            return new TableClient(_serviceClient.Uri, tableName, new DefaultAzureCredential());
        }

        /// <summary>
        /// Provides a table that is currently available remotely.
        /// </summary>
        public async Task<TableClient> WhenTableAvailableAsync()
        {
            TableClient client = WhenTableUnavailable();

            _logger.LogTrace("[Test] create Azure table '{TableName}'", client.Name);
            await client.CreateIfNotExistsAsync();

            return client;
        }

        /// <summary>
        /// Provides a table entity that is currently not available remotely.
        /// </summary>
        public TableEntity WhenTableEntityUnavailable()
        {
            string CreateKey() => "prop" + Bogus.Random.Guid().ToString("N");
            var entity = new TableEntity(Bogus.Random.Guid().ToString(), Bogus.Random.Guid().ToString())
            {
                { CreateKey(), Bogus.Random.Int() },
                { CreateKey(), Bogus.Random.Guid() },
                { CreateKey(), Bogus.Random.Double() },
                { CreateKey(), Bogus.Random.Bytes(10) }
            };

            return entity;
        }

        /// <summary>
        /// Provides a table entity that is currently available remotely.
        /// </summary>
        public async Task<TableEntity> WhenTableEntityAvailableAsync(TableClient client)
        {
            TableEntity entity = WhenTableEntityUnavailable();
            await client.AddEntityAsync(entity);

            return entity;
        }

        /// <summary>
        /// Makes sure that the given table is deleted remotely.
        /// </summary>
        public async Task WhenTableDeletedAsync(TableClient client)
        {
            await client.DeleteAsync();
        }

        /// <summary>
        /// Makes sure that the given table entity is deleted remotely.
        /// </summary>
        public async Task WhenTableEntityDeletedAsync(TableClient client, TableEntity entity)
        {
            await client.DeleteEntityAsync(entity);
        }

        /// <summary>
        /// Verifies that a table is available remotely.
        /// </summary>
        public async Task ShouldStoreTableAsync(TableClient table)
        {
            bool exists = false;
            await foreach (TableItem _ in _serviceClient.QueryAsync(t => t.Name == table.Name))
            {
                exists = true;
            }

            Assert.True(exists, $"Azure table '{table.Name}' should be available");
        }

        /// <summary>
        /// Verifies that a table is not available remotely.
        /// </summary>
        public async Task ShouldNotStoreTableAsync(TableClient table)
        {
            bool exists = false;
            await foreach (TableItem _ in _serviceClient.QueryAsync(t => t.Name == table.Name))
            {
                exists = true;
            }

            Assert.False(exists, $"Azure table '{table.Name}' should not be available");
        }

        /// <summary>
        /// Verifies that a table entity is available remotely.
        /// </summary>
        public async Task ShouldStoreTableEntityAsync(TableClient table, TableEntity entity)
        {
            NullableResponse<TableEntity> response = await table.GetEntityIfExistsAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            Assert.True(response.HasValue, $"Azure Table entity should be available in table '{table.Name}'");

            Assert.NotNull(response.Value);


            Assert.All(entity, item => Assert.Equal(item.Value, Assert.Contains(item.Key, response.Value)));
        }

        /// <summary>
        /// Verifies that a table entity is not available remotely.
        /// </summary>
        public async Task ShouldNotStoreTableEntityAsync(TableClient table, TableEntity entity)
        {
            NullableResponse<TableEntity> response = await table.GetEntityIfExistsAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            Assert.False(response.HasValue, $"Azure Table entity should not be available in table '{table.Name}'");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.AddRange(_tableNames.Select(tableName =>
            {
                return AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("[Test] delete Azure table '{TableName}'", tableName);
                    await _serviceClient.DeleteTableAsync(tableName);
                });
            }));

            disposables.Add(_connection);
        }
    }
}
