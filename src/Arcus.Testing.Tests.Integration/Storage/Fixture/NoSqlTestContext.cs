﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Azure;
using Azure.Identity;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Storage.Fixture
{
    public interface INoSqlItem
    {
        string GetId();
        PartitionKey GetPartitionKey();
    }

    public class NoSqlTestContext : IAsyncDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;
        private readonly NoSqlConfig _config;
        private readonly CosmosClient _client;
        public readonly Database _database;
        private readonly Collection<string> _containerNames = new();
        private readonly ILogger _logger;

        private NoSqlTestContext(
            TemporaryManagedIdentityConnection connection,
            CosmosClient client,
            Database database,
            NoSqlConfig config,
            ILogger logger)
        {
            _connection = connection;
            _config = config;
            _client = client;
            _database = database;
            _logger = logger;
        }

        public static async Task<NoSqlTestContext> GivenAsync(TestConfig config, ILogger logger)
        {
            var connection = TemporaryManagedIdentityConnection.Create(config.GetServicePrincipal());
            NoSqlConfig noSql = config.GetNoSql();

            var client = new CosmosClient(noSql.Endpoint, new DefaultAzureCredential());
            Database database = client.GetDatabase(noSql.DatabaseName);

            return new NoSqlTestContext(connection, client, database, noSql, logger);
        }

        public string WhenContainerNameUnavailable()
        {
            string containerNames = $"test-{Guid.NewGuid()}";
            _containerNames.Add(containerNames);

            return containerNames;
        }

        public async Task<string> WhenContainerNameAvailableAsync(string partitionKeyPath = "/pk")
        {
            string containerName = WhenContainerNameUnavailable();
            _logger.LogTrace("[Test] create NoSql container '{ContainerName}' outside the fixture's scope", containerName);

            var arm = new ArmClient(new DefaultAzureCredential());
            CosmosDBAccountResource cosmosDb = arm.GetCosmosDBAccountResource(_config.ResourceId);
            CosmosDBSqlDatabaseResource database = await cosmosDb.GetCosmosDBSqlDatabaseAsync(_config.DatabaseName);

            CosmosDBAccountResource resource = await cosmosDb.GetAsync();
            var newContainer = new CosmosDBSqlContainerResourceInfo(containerName)
            {
                PartitionKey = new CosmosDBContainerPartitionKey { Paths = { partitionKeyPath }}
            };
            var request = new CosmosDBSqlContainerCreateOrUpdateContent(resource.Data.Location, newContainer);
            await database.GetCosmosDBSqlContainers()
                          .CreateOrUpdateAsync(WaitUntil.Completed, containerName, request);

            return containerName;
        }

        public async Task WhenContainerDeletedAsync(string containerName)
        {
            _logger.LogTrace("[Test] delete NoSql container '{ContainerName}' outside test fixture's scope", containerName);

            var arm = new ArmClient(new DefaultAzureCredential());
            CosmosDBAccountResource cosmosDb = arm.GetCosmosDBAccountResource(_config.ResourceId);
            CosmosDBSqlDatabaseResource database = await cosmosDb.GetCosmosDBSqlDatabaseAsync(_config.DatabaseName);

            CosmosDBSqlContainerResource container = await database.GetCosmosDBSqlContainerAsync(containerName);
            await container.DeleteAsync(WaitUntil.Completed);
        }

        public async Task<T> WhenItemAvailableAsync<T>(string containerName, T item) where T : INoSqlItem
        {
            _logger.LogTrace("[Test] add '{ItemType}' item '{ItemId}' to NoSql container '{ContainerName}'", typeof(T).Name, item.GetId(), containerName);

            Container container = _database.GetContainer(containerName);
            await using var stream = _client.ClientOptions.Serializer.ToStream(item);
            await container.CreateItemStreamAsync(stream, item.GetPartitionKey());

            return item;
        }

        public async Task ShouldStoreContainerAsync(string containerId)
        {
            Container cont = _database.GetContainer(containerId);
            ContainerProperties properties = await cont.ReadContainerAsync();
            Assert.NotNull(properties);
        }

        public async Task ShouldNotStoreContainerAsync(string containerId)
        {
            Container cont = _database.GetContainer(containerId);
            var exception = await Assert.ThrowsAnyAsync<CosmosException>(() => cont.ReadContainerAsync());
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        public async Task ShouldStoreItemAsync<T>(string containerName, T expected, Action<T> assertion = null) where T : INoSqlItem
        {
            Container container = _database.GetContainer(containerName);
            await Poll.UntilAvailableAsync(async () =>
            {
                ItemResponse<T> response = await container.ReadItemAsync<T>(expected.GetId(), expected.GetPartitionKey());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                assertion?.Invoke(response.Resource);
            });
        }

        public async Task ShouldNotStoreItemAsync<T>(string containerName, T expected) where T : INoSqlItem
        {
            Container container = _database.GetContainer(containerName);
            await Poll.UntilAvailableAsync(async () =>
            {
                using ResponseMessage response = await container.ReadItemStreamAsync(expected.GetId(), expected.GetPartitionKey());
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
        }

        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.Add(_connection);
            disposables.Add(AsyncDisposable.Create(async () =>
            {
                var arm = new ArmClient(new DefaultAzureCredential());
                CosmosDBAccountResource cosmosDb = arm.GetCosmosDBAccountResource(_config.ResourceId);
                CosmosDBSqlDatabaseResource database = await cosmosDb.GetCosmosDBSqlDatabaseAsync(_config.DatabaseName);

                foreach (var containerName in _containerNames)
                {
                    try
                    {
                        _logger.LogTrace("[Test] delete NoSql container '{ContainerName}'", containerName);
                        CosmosDBSqlContainerResource container = await database.GetCosmosDBSqlContainerAsync(containerName);
                        await container.DeleteAsync(WaitUntil.Started);
                    }
                    catch (CosmosException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
                    {
                        // Ignore when the container does not exists.
                    }
                    catch (RequestFailedException exception) when (exception.Status is 404)
                    {
                        // Ignore when the client is not active anymore.
                    }
                }
            }));
            disposables.Add(AsyncDisposable.Create(() =>
            {
                try
                {
                    _client.Dispose();
                }
                catch (RequestFailedException exception) when (exception.Status is 404)
                {
                    // Ignore when the client is not active anymore.
                }
            }));
        }
    }
}
