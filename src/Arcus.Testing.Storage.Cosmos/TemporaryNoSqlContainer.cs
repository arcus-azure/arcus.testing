using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Represents a generic dictionary-like type which defines an arbitrary set of properties on a NoSql item as key-value pairs.
    /// </summary>
    public class NoSqlItem : JObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoSqlItem" /> class.
        /// </summary>
        internal NoSqlItem(
            string id,
            PartitionKey partitionKey,
            JObject properties)
            : base(properties)
        {
            ArgumentNullException.ThrowIfNull(id);

            Id = id;
            PartitionKey = partitionKey;
        }

        /// <summary>
        /// Gets the unique identifier to distinguish items.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the key to group items together in partitions.
        /// </summary>
        public PartitionKey PartitionKey { get; }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class OnSetupNoSqlContainerOptions
    {
        private readonly List<Func<CosmosClient, NoSqlItem, bool>> _filters = new();

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
        public OnSetupNoSqlContainerOptions CleanMatchingItems(params Func<NoSqlItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnSetupNoSqlContainer.CleanIfMatched;
            _filters.AddRange(filters.Select(f => new Func<CosmosClient, NoSqlItem, bool>((_, item) => f(item))));

            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSql items
        /// upon the test fixture creation that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will be aggregated together in an OR expression.
        /// </remarks>
        /// <typeparam name="TItem">The custom type of the NoSql item.</typeparam>
        /// <param name="filters">The filters to match NoSql items that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more <paramref name="filters"/> is <c>null</c>.</exception>
        public OnSetupNoSqlContainerOptions CleanMatchingItems<TItem>(params Func<TItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnSetupNoSqlContainer.CleanIfMatched;

            _filters.AddRange(filters.Select(itemFilter => new Func<CosmosClient, NoSqlItem, bool>((client, json) =>
            {
                var item = NoSqlItemParser.Parse<TItem>(client, json, TestPhase.Setup);
                return itemFilter(item);

            })));

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current NoSql item.
        /// </summary>
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JObject itemStream, CosmosClient client)
        {
            var item = new NoSqlItem(itemId, partitionKey, itemStream);
            return _filters.Exists(filter => filter(client, item));
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class OnTeardownNoSqlContainerOptions
    {
        private readonly List<Func<CosmosClient, NoSqlItem, bool>> _filters = new();

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
        public OnTeardownNoSqlContainerOptions CleanMatchingItems(params Func<NoSqlItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnTeardownNoSqlContainer.CleanIfMatched;
            _filters.AddRange(filters.Select(f => new Func<CosmosClient, NoSqlItem, bool>((_, item) => f(item))));

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
        /// <typeparam name="TItem">The custom type of the NoSql item.</typeparam>
        /// <param name="filters">The filters  to match NoSql items that should be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains <c>null</c>.</exception>
        public OnTeardownNoSqlContainerOptions CleanMatchingItems<TItem>(params Func<TItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Items = OnTeardownNoSqlContainer.CleanIfMatched;

            _filters.AddRange(filters.Select(itemFilter => new Func<CosmosClient, NoSqlItem, bool>((client, json) =>
            {
                var item = NoSqlItemParser.Parse<TItem>(client, json, TestPhase.Teardown);
                return itemFilter(item);
            })));

            return this;
        }

        /// <summary>
        /// Determine if any of the user configured filters matches with the current NoSql item.
        /// </summary>
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JObject itemStream, CosmosClient client)
        {
            var item = new NoSqlItem(itemId, partitionKey, itemStream);
            return _filters.Exists(filter => filter(client, item));
        }
    }

    internal enum TestPhase { Setup, Teardown }

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
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
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
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
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
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
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
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos resource where the temporary NoSql container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
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
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(partitionKeyPath);
            logger ??= NullLogger.Instance;

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
                    logger.LogDebug("[Test:Setup] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in container '{DatabaseName}/{ContainerName}'", id, partitionKey, container.Database.Id, container.Id);
                    using ResponseMessage response = await container.DeleteItemStreamAsync(id, partitionKey);

                    if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw new RequestFailedException(
                            $"[Test:Setup] Failed to delete Azure Cosmos NoSql item '{id}' {partitionKey} in container '{container.Database.Id}/{container.Id}' " +
                            $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                    }
                });
            }
            else if (options.OnSetup.Items is OnSetupNoSqlContainer.CleanIfMatched)
            {
                await ForEachItemAsync(container, async (id, key, doc) =>
                {
                    if (options.OnSetup.IsMatched(id, key, doc, container.Database.Client))
                    {
                        logger.LogDebug("[Test:Setup] Delete matched Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in container '{DatabaseName}/{ContainerName}'", id, key, container.Database.Id, container.Id);
                        using ResponseMessage response = await container.DeleteItemStreamAsync(id, key);

                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                        {
                            throw new RequestFailedException(
                                $"[Test:Setup] Failed to delete matched Azure Cosmos NoSql item '{id}' {key} in container '{container.Database.Id}/{container.Id}' " +
                                $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                        }
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
                await CleanContainerOnTeardownAsync(disposables);
            }

            disposables.Add(_resourceClient);

            GC.SuppressFinalize(this);
        }

        private async Task CleanContainerOnTeardownAsync(DisposableCollection disposables)
        {
            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfCreated)
            {
                return;
            }

            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanAll)
            {
                await ForEachItemAsync((id, key, _) =>
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogDebug("[Test:Teardown] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in NoSql container '{DatabaseName}/{ContainerName}'", id, key, Client.Database.Id, Client.Id);
                        using ResponseMessage response = await Client.DeleteItemStreamAsync(id, key);

                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                        {
                            throw new RequestFailedException(
                                $"[Test:Teardown] Failed to delete Azure Cosmos NoSql item '{id}' {key} in container '{Client.Database.Id}/{Client.Id}' " +
                                $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                        }
                    }));

                    return Task.CompletedTask;
                });
            }
            else if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfMatched)
            {
                await ForEachItemAsync((id, key, doc) =>
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        if (_options.OnTeardown.IsMatched(id, key, doc, Client.Database.Client))
                        {
                            _logger.LogDebug("[Test:Teardown] Delete Azure Cosmos NoSql item '{ItemId}' {PartitionKey} in NoSql container '{DatabaseName}/{ContainerName}'", id, key, Client.Database.Id, Client.Id);
                            using ResponseMessage response = await Client.DeleteItemStreamAsync(id, key);

                            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                            {
                                throw new RequestFailedException(
                                    $"[Test:Teardown] Failed to delete matched Azure Cosmos NoSql item '{id}' {key} in container '{Client.Database.Id}/{Client.Id}' " +
                                    $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                            }
                        }
                    }));

                    return Task.CompletedTask;
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
