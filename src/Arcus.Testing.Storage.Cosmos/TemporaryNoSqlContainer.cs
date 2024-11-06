using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Arcus.Testing.NoSqlExtraction;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryNoSqlContainer"/> is created.
    /// </summary>
    internal enum OnSetupNoSqlContainer { LeaveExisted = 0, CleanIfExisted, CleanIfMatched }
    
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryNoSqlContainer"/> is deleted.
    /// </summary>
    internal enum OnTeardownNoSqlContainer { CleanIfCreated = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents a filter to match against a stored NoSql item in the <see cref="TemporaryNoSqlContainer"/> upon setup or teardown.
    /// </summary>
    public class NoSqlItemFilter
    {
        private readonly Func<string, PartitionKey, JObject, CosmosClient, bool> _filter;

        private NoSqlItemFilter(Func<string, PartitionKey, JObject, CosmosClient, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _filter = filter;
        }

        /// <summary>
        /// Creates a filter to match a NoSql item by its unique item ID.
        /// </summary>
        /// <param name="itemId">The unique required 'id' value.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="itemId"/> is blank.</exception>
        public static NoSqlItemFilter IdEqual(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Requires non-blank NoSql item ID to match against stored items", nameof(itemId));
            }

            return new NoSqlItemFilter((id, _, _, _) => itemId.Equals(id));
        }

        /// <summary>
        /// Creates a filter to match a NoSql item by its unique item ID.
        /// </summary>
        /// <param name="itemId">The unique required 'id' value.</param>
        /// <param name="comparisonType">The value that specifies how the strings will be compared.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="itemId"/> is blank.</exception>
        public static NoSqlItemFilter IdEqual(string itemId, StringComparison comparisonType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Requires non-blank NoSql item ID to match against stored items", nameof(itemId));
            }

            return new NoSqlItemFilter((id, _, _, _) => itemId.Equals(id, comparisonType));
        }

        /// <summary>
        /// Creates a filter to match a NoSql item by its partition key.
        /// </summary>
        /// <param name="partitionKey">The key in which the item is partitioned.</param>
        public static NoSqlItemFilter PartitionKeyEqual(PartitionKey partitionKey)
        {
            return new NoSqlItemFilter((_, key, _, _) => key.Equals(partitionKey));
        }

        /// <summary>
        /// Creates a filter to match a NoSql item based on its contents.
        /// </summary>
        /// <typeparam name="TItem">The custom type of the NoSql item.</typeparam>
        /// <param name="itemFilter">The custom filter to match against the contents of the NoSql item.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="itemFilter"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the used client has no serializer configured.</exception>
        public static NoSqlItemFilter Where<TItem>(Func<TItem, bool> itemFilter)
        {
            ArgumentNullException.ThrowIfNull(itemFilter);

            return new NoSqlItemFilter((_, _, json, client) =>
            {
                if (client.ClientOptions.Serializer is null)
                {
                    throw new InvalidOperationException(
                        "Cannot match the NoSql item because the Cosmos client used has no JSON item serializer configured");
                }

                using var body = new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()));
                
                var item = client.ClientOptions.Serializer.FromStream<TItem>(body);
                if (item is null)
                {
                    throw new InvalidOperationException(
                        $"Cannot match the NoSql item because the configured JSON item serializer returned 'null' when deserializing '{typeof(TItem).Name}'");
                }

                return itemFilter(item);
            });
        }

        /// <summary>
        /// Creates a filter to match a NoSql item based on its contents.
        /// </summary>
        /// <param name="itemFilter">The custom filter to match against the contents of the NoSql item.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="itemFilter"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the used client has no serializer configured.</exception>
        public static NoSqlItemFilter Where(Func<IDictionary<string, object>, bool> itemFilter)
        {
            ArgumentNullException.ThrowIfNull(itemFilter);

            return new NoSqlItemFilter((_, _, json, _) =>
            {
                var dic = json.ToObject<Dictionary<string, object>>();
                return itemFilter(dic);
            });
        }

        /// <summary>
        /// Match the current NoSql item with the user configured filter.
        /// </summary>
        internal bool IsMatch(string itemId, PartitionKey partitionKey, JObject item, CosmosClient client)
        {
            return _filter(itemId, partitionKey, item, client);
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class OnSetupNoSqlContainerOptions
    {
        private readonly List<NoSqlItemFilter> _filters = new();

        /// <summary>
        /// Gets the configurable setup option on what to do with existing NoSql items in the Azure NoSql container upon the test fixture creation.
        /// </summary>
        internal OnSetupNoSqlContainer Items { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryNoSqlContainer"/> to leave all NoSql items untouched
        /// that already existed upon the test fixture creation, when there was already an Azure NoSql container available.
        /// </summary>
        public OnSetupNoSqlContainerOptions LeaveAllItems()
        {
            Items = OnSetupNoSqlContainer.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete all the already existing NoSql items upon the test fixture creation.
        /// </summary>
        public OnSetupNoSqlContainerOptions CleanAllItems()
        {
            Items = OnSetupNoSqlContainer.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSql items
        /// upon the test fixture creation that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will be aggregated together in an OR expression.
        /// </remarks>
        /// <param name="filters">The filters to match NoSql items that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more <paramref name="filters"/> is <c>null</c>.</exception>
        public OnSetupNoSqlContainerOptions CleanMatchingItems(params NoSqlItemFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnSetupNoSqlContainer.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current NoSql item.
        /// </summary>
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JObject itemStream, CosmosClient client)
        {
            return _filters.Exists(filter => filter.IsMatch(itemId, partitionKey, itemStream, client));
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class OnTeardownNoSqlContainerOptions
    {
        private readonly List<NoSqlItemFilter> _filters = new();

        /// <summary>
        /// Gets the configurable setup option on what to do with existing NoSql items in the Azure NoSql container upon the test fixture deletion.
        /// </summary>
        internal OnTeardownNoSqlContainer Items { get; private set; }

        /// <summary>
        /// (default for cleaning items) Configures the <see cref="TemporaryNoSqlContainer"/> to only delete the NoSql items upon disposal
        /// if the item was inserted by the test fixture (using <see cref="TemporaryNoSqlContainer.AddItemAsync{TItem}"/>).
        /// </summary>
        public OnTeardownNoSqlContainerOptions CleanCreatedItems()
        {
            Items = OnTeardownNoSqlContainer.CleanIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete all the NoSql items upon disposal - even if the test fixture didn't add them.
        /// </summary>
        public OnTeardownNoSqlContainerOptions CleanAllItems()
        {
            Items = OnTeardownNoSqlContainer.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSql items
        /// upon disposal that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     The matching of items only happens on NoSql items that were created outside the scope of the test fixture.
        ///     All items created by the test fixture will be deleted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        /// </remarks>
        /// <param name="filters">The filters  to match NoSql items that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains <c>null</c>.</exception>
        public OnTeardownNoSqlContainerOptions CleanMatchingItems(params NoSqlItemFilter[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnTeardownNoSqlContainer.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current NoSql item.
        /// </summary>
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JObject itemStream, CosmosClient client)
        {
            return _filters.Exists(filter => filter.IsMatch(itemId, partitionKey, itemStream, client));
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class TemporaryNoSqlContainerOptions
    {
        /// <summary>
        /// Gets the additional options to manipulate the creation of the <see cref="TemporaryNoSqlContainer"/>.
        /// </summary>
        public OnSetupNoSqlContainerOptions OnSetup { get; } = new OnSetupNoSqlContainerOptions().LeaveAllItems();

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryNoSqlContainer"/>.
        /// </summary>
        public OnTeardownNoSqlContainerOptions OnTeardown { get; } = new OnTeardownNoSqlContainerOptions().CleanCreatedItems();
    }

    /// <summary>
    /// Represents a temporary Azure Cosmos NoSql container that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryNoSqlContainer : IAsyncDisposable
    {
        private readonly CosmosDBSqlContainerResource _container;
        private readonly CosmosClient _resourceClient;
        private readonly bool _createdByUs;
        private readonly Collection<IAsyncDisposable> _items = new();
        private readonly TemporaryNoSqlContainerOptions _options;
        private readonly ILogger _logger;

        private TemporaryNoSqlContainer(
            CosmosClient resourceClient,
            Container containerClient,
            CosmosDBSqlContainerResource container,
            bool createdByUs,
            TemporaryNoSqlContainerOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(containerClient);
            ArgumentNullException.ThrowIfNull(resourceClient);
            ArgumentNullException.ThrowIfNull(options);

            _container = container;
            _createdByUs = createdByUs;
            _options = options;
            _resourceClient = resourceClient;
            _logger = logger ?? NullLogger.Instance;

            Client = containerClient;
        }

        /// <summary>
        /// Gets the unique name of the NoSql container, currently available on Azure Cosmos.
        /// </summary>
        public string Name => Client.Id;

        /// <summary>
        /// Gets the client to interact with the NoSql container.
        /// </summary>
        public Container Client { get; }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryNoSqlContainer"/>.
        /// </summary>
        public OnTeardownNoSqlContainerOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos NoSql container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</param>
        /// <param name="databaseName">The name of the existing NoSql database in the Azure Cosmos resource.</param>
        /// <param name="containerName">The name of the NoSql container to be created within the Azure Cosmos resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSql item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSql container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbAccountResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static async Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos NoSql container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</param>
        /// <param name="databaseName">The name of the existing NoSql database in the Azure Cosmos resource.</param>
        /// <param name="containerName">The name of the NoSql container to be created within the Azure Cosmos resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSql item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSql container.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbAccountResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static async Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger,
            Action<TemporaryNoSqlContainerOptions> configureOptions)
        {
            return await CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                new DefaultAzureCredential(),
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos NoSql container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</param>
        /// <param name="credential">The credential implementation to authenticate with the Azure Cosmos resource.</param>
        /// <param name="databaseName">The name of the existing NoSql database in the Azure Cosmos resource.</param>
        /// <param name="containerName">The name of the NoSql container to be created within the Azure Cosmos resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSql item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSql container.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="cosmosDbAccountResourceId"/> or the <paramref name="credential"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static async Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            TokenCredential credential,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                credential,
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos NoSql container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</param>
        /// <param name="credential">The credential implementation to authenticate with the Azure Cosmos resource.</param>
        /// <param name="databaseName">The name of the existing NoSql database in the Azure Cosmos resource.</param>
        /// <param name="containerName">The name of the NoSql container to be created within the Azure Cosmos resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSql item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSql container.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="cosmosDbAccountResourceId"/> or the <paramref name="credential"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static async Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            TokenCredential credential,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger,
            Action<TemporaryNoSqlContainerOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(cosmosDbAccountResourceId);
            ArgumentNullException.ThrowIfNull(credential);
            logger ??= NullLogger.Instance;

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException(
                    "Requires a non-blank name for the NoSql database in the Azure Cosmos resource", nameof(databaseName));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException(
                    "Requires a non-blank name for the temporary NoSql container in the Azure Cosmos resource", nameof(containerName));
            }

            if (string.IsNullOrWhiteSpace(partitionKeyPath))
            {
                throw new ArgumentException(
                    "Requires a non-blank path to the partition key for the temporary NoSql container", nameof(partitionKeyPath));
            }

            var options = new TemporaryNoSqlContainerOptions();
            configureOptions?.Invoke(options);

            CosmosDBAccountResource cosmosDb = await GetCosmosDbResourceAsync(cosmosDbAccountResourceId, credential);
            CosmosDBSqlDatabaseResource database = await cosmosDb.GetCosmosDBSqlDatabaseAsync(databaseName);

            var cosmosClient = new CosmosClient(cosmosDb.Data.DocumentEndpoint, credential);
            Container containerClient = cosmosClient.GetContainer(databaseName, containerName);

            if (await database.GetCosmosDBSqlContainers().ExistsAsync(containerName))
            {
                logger.LogDebug("[Test:Setup] Use already existing Azure Cosmos NoSql '{ContainerName}' container in database '{DatabaseName}'", containerName, databaseName);
                await CleanContainerOnSetupAsync(containerClient, options, logger);

                CosmosDBSqlContainerResource container = await database.GetCosmosDBSqlContainerAsync(containerName);
                return new TemporaryNoSqlContainer(cosmosClient, containerClient, container, createdByUs: false, options, logger);
            }
            else
            {
                logger.LogDebug("[Test:Setup] Create new Azure Cosmos NoSql '{ContainerName}' container in database '{DatabaseName}'", containerName, databaseName);

                var properties = new ContainerProperties(containerName, partitionKeyPath);
                CosmosDBSqlContainerResource container = await CreateNewNoSqlContainerAsync(cosmosDb, database, properties);
                
                return new TemporaryNoSqlContainer(cosmosClient, containerClient, container, createdByUs: true, options, logger);
            }
        }

        private static async Task<CosmosDBSqlContainerResource> CreateNewNoSqlContainerAsync(
            CosmosDBAccountResource cosmosDb,
            CosmosDBSqlDatabaseResource database,
            ContainerProperties properties)
        {
            var partitionKey = new CosmosDBContainerPartitionKey();
            foreach (string path in properties.PartitionKeyPaths)
            {
                partitionKey.Paths.Add(path);
            }

            var newContainer = new CosmosDBSqlContainerResourceInfo(properties.Id)
            {
                PartitionKey = partitionKey
            };
            var request = new CosmosDBSqlContainerCreateOrUpdateContent(cosmosDb.Data.Location, newContainer);
            await database.GetCosmosDBSqlContainers()
                          .CreateOrUpdateAsync(WaitUntil.Completed, properties.Id, request);

            return await database.GetCosmosDBSqlContainerAsync(properties.Id);
        }

        private static async Task<CosmosDBAccountResource> GetCosmosDbResourceAsync(ResourceIdentifier cosmosDbAccountResourceId, TokenCredential credential)
        {
            var arm = new ArmClient(credential);
            CosmosDBAccountResource cosmosDb = arm.GetCosmosDBAccountResource(cosmosDbAccountResourceId);
            
            return await cosmosDb.GetAsync();
        }

        private static async Task CleanContainerOnSetupAsync(Container container, TemporaryNoSqlContainerOptions options, ILogger logger)
        {
            if (options.OnSetup.Items is OnSetupNoSqlContainer.LeaveExisted)
            {
                return;
            }
            
            if (options.OnSetup.Items is OnSetupNoSqlContainer.CleanIfExisted)
            {
                await ForEachItemAsync(container, async (id, partitionKey, _) =>
                {
                    logger.LogTrace("[Test:Setup] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in container '{DatabaseName}/{ContainerName}'", id, partitionKey, container.Database.Id, container.Id);
                    await container.DeleteItemStreamAsync(id, partitionKey);
                });
            }
            else if (options.OnSetup.Items is OnSetupNoSqlContainer.CleanIfMatched)
            {
                await ForEachItemAsync(container, async (id, key, doc) =>
                {
                    if (options.OnSetup.IsMatched(id, key, doc, container.Database.Client))
                    {
                        logger.LogTrace("[Test:Setup] Delete matched Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in container '{DatabaseName}/{ContainerName}'", id, key, container.Database.Id, container.Id);
                        await container.DeleteItemStreamAsync(id, key);
                    }
                });
            }
        }

        /// <summary>
        /// Adds a temporary NoSql item to the current container instance.
        /// </summary>
        /// <typeparam name="T">The custom NoSql model.</typeparam>
        /// <param name="item">The item to create in the NoSql container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="item"/> is <c>null</c>.</exception>
        public async Task AddItemAsync<T>(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _items.Add(await TemporaryNoSqlItem.InsertIfNotExistsAsync(Client, item, _logger));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            disposables.AddRange(_items);

            if (_createdByUs)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogDebug("[Test:Teardown] Delete Azure Cosmos NoSql '{ContainerName}' container in database '{DatabaseName}'", _container.Id.Name, _container.Id.Parent?.Name);
                    await _container.DeleteAsync(WaitUntil.Completed);
                })); 
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await CleanContainerOnTeardownAsync();
                }));
            }

            disposables.Add(_resourceClient);

            GC.SuppressFinalize(this);
        }

        private async Task CleanContainerOnTeardownAsync()
        {
            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfCreated)
            {
                return;
            }

            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanAll)
            {
                await ForEachItemAsync(async (id, key, _) =>
                {
                    _logger.LogTrace("[Test:Teardown] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in NoSql container '{DatabaseName}/{ContainerName}'", id, key, Client.Database.Id, Client.Id);
                    await Client.DeleteItemStreamAsync(id, key);
                });
            }
            else if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfMatched)
            {
                await ForEachItemAsync(async (id, key, doc) =>
                {
                    if (_options.OnTeardown.IsMatched(id, key, doc, Client.Database.Client))
                    {
                        _logger.LogTrace("[Test:Teardown] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in NoSql container '{DatabaseName}/{ContainerName}'", id, key, Client.Database.Id, Client.Id);
                        await Client.DeleteItemStreamAsync(id, key);
                    }
                });
            }
        }

        private async Task ForEachItemAsync(Func<string, PartitionKey, JObject, Task> deleteItemAsync)
        {
            await ForEachItemAsync(Client, deleteItemAsync);
        }

        private static async Task ForEachItemAsync(Container container, Func<string, PartitionKey, JObject, Task> deleteItemAsync)
        {
            ContainerResponse resp = await container.ReadContainerAsync();
            ContainerProperties properties = resp.Resource;

            using FeedIterator iterator = container.GetItemQueryStreamIterator();
            while (iterator.HasMoreResults)
            {
                using ResponseMessage message = await iterator.ReadNextAsync();
                if (!message.IsSuccessStatusCode)
                {
                    continue;
                }

                using var content = new StreamReader(message.Content);
                using var reader = new JsonTextReader(content);

                JToken json = await JToken.ReadFromAsync(reader);
                if (json is not JObject root
                    || !root.TryGetValue("Documents", out JToken docs)
                    || docs is not JArray docsArr)
                {
                    continue;
                }

                foreach (JObject doc in docsArr.Where(d => d is JObject).Cast<JObject>().ToArray())
                {
                    string id = ExtractIdFromItem(doc);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    PartitionKey partitionKey = ExtractPartitionKeyFromItem(doc, properties);
                    await deleteItemAsync(id, partitionKey, doc);
                }
            }
        }
    }
}
