using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Azure.Storage.Blobs;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    public class TableStorageTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly Collection<string> _tableNames = new();
        private readonly TableServiceClient _serviceClient;
        private readonly StorageAccount _storageAccount;
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private TableStorageTestContext(
            TemporaryManagedIdentityConnection connection,
            TableServiceClient serviceClient,
            StorageAccount storageAccount,
            ILogger logger)
        {
            _connection = connection;
            _serviceClient = serviceClient;
            _storageAccount = storageAccount;
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

            return Task.FromResult(new TableStorageTestContext(connection, serviceClient, storageAccount, logger));
        }

        public TableClient WhenTableUnavailable()
        {
            string tableName = $"table{Guid.NewGuid()}".Replace("-", "");
            _tableNames.Add(tableName);

            return new TableClient(_serviceClient.Uri, tableName, new DefaultAzureCredential());
        }

        public async Task<TableClient> WhenTableAvailableAsync()
        {
            TableClient client = WhenTableUnavailable();

            _logger.LogTrace("[Test] create Azure table '{TableName}'", client.Name);
            await client.CreateIfNotExistsAsync();

            return client;
        }

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

        public async Task<TableEntity> WhenTableEntityAvailableAsync(TableClient client)
        {
            TableEntity entity = WhenTableEntityUnavailable();
            await client.AddEntityAsync(entity);

            return entity;
        }

        public async Task WhenTableDeletedAsync(TableClient client)
        {
            await client.DeleteAsync();
        }

        public async Task WhenTableEntityDeletedAsync(TableClient client, TableEntity entity)
        {
            await client.DeleteEntityAsync(entity);
        }

        public async Task ShouldStoreTableAsync(TableClient table)
        {
            bool exists = false;
            await foreach (TableItem _ in _serviceClient.QueryAsync(t => t.Name == table.Name))
            {
                exists = true;
            }

            Assert.True(exists, $"Azure table '{table.Name}' should be available");
        }

        public async Task ShouldNotStoreTableAsync(TableClient table)
        {
            bool exists = false;
            await foreach (TableItem _ in _serviceClient.QueryAsync(t => t.Name == table.Name))
            {
                exists = true;
            }

            Assert.False(exists, $"Azure table '{table.Name}' should not be available");
        }

        public async Task ShouldStoreTableEntityAsync(TableClient table, TableEntity entity)
        {
            NullableResponse<TableEntity> response = await table.GetEntityIfExistsAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            Assert.True(response.HasValue, $"Azure table entity should be available in table '{table.Name}'");

            Assert.NotNull(response.Value);
            Assert.All(entity, item => Assert.Equal(item.Value, Assert.Contains(item.Key, response.Value)));
        }

        public async Task ShouldNotStoreTableEntityAsync(TableClient table, TableEntity entity)
        {
            NullableResponse<TableEntity> response = await table.GetEntityIfExistsAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            Assert.False(response.HasValue, $"Azure table entity should not be available in table '{table.Name}'");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            foreach (string tableName in _tableNames)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("[Test] delete Azure table '{TableName}'", tableName);
                    await _serviceClient.DeleteTableAsync(tableName);
                }));
            }
            disposables.Add(_connection);
        }
    }
}
