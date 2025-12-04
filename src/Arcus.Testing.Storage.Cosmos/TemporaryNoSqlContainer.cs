using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
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
    internal enum OnTeardownNoSqlContainer { CleanIfUpserted = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents a generic dictionary-like type which defines an arbitrary set of properties on an Azure Cosmos DB for NoSQL item as key-value pairs.
    /// </summary>
    public class NoSqlItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoSqlItem" /> class.
        /// </summary>
        internal NoSqlItem(
            string id,
            PartitionKey partitionKey,
            JsonObject properties)
        {
            ArgumentNullException.ThrowIfNull(id);

            Id = id;
            PartitionKey = partitionKey;
            Content = properties;
        }

        /// <summary>
        /// Gets the unique identifier to distinguish items.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the key to group items together in partitions.
        /// </summary>
        public PartitionKey PartitionKey { get; }

        /// <summary>
        /// Gets the complete custom user-defined content of the stored NoSQL item.
        /// </summary>
        public JsonNode Content { get; }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryNoSqlContainer"/>.
    /// </summary>
    public class OnSetupNoSqlContainerOptions
    {
        private readonly List<Func<CosmosClient, NoSqlItem, bool>> _filters = [];

        /// <summary>
        /// Gets the configurable setup option on what to do with existing NoSQL items in the Azure NoSql container upon the test fixture creation.
        /// </summary>
        internal OnSetupNoSqlContainer Items { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryNoSqlContainer"/> to leave all NoSQL items untouched
        /// that already existed upon the test fixture creation, when there was already an Azure Cosmos DB for NoSQL container available.
        /// </summary>
        public OnSetupNoSqlContainerOptions LeaveAllItems()
        {
            Items = OnSetupNoSqlContainer.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete all the already existing NoSQL items
        /// in an Azure Cosmos DB for NoSql container upon the test fixture creation.
        /// </summary>
        public OnSetupNoSqlContainerOptions CleanAllItems()
        {
            Items = OnSetupNoSqlContainer.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSQL items
        /// in an Azure Cosmos DB for NoSql container upon the test fixture creation that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will be aggregated together in an OR expression.
        /// </remarks>
        /// <param name="filters">The filters to match NoSQL items that should be removed.</param>
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
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSQL items
        /// in an Azure Cosmos DB for NoSql container upon the test fixture creation that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     Multiple calls will be aggregated together in an OR expression.
        /// </remarks>
        /// <typeparam name="TItem">The custom type of the NoSQL item.</typeparam>
        /// <param name="filters">The filters to match NoSQL items that should be removed.</param>
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
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JsonObject itemStream, CosmosClient client)
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
        private readonly List<Func<CosmosClient, NoSqlItem, bool>> _filters = [];

        /// <summary>
        /// Gets the configurable setup option on what to do with existing NoSQL items in the Azure NoSql container upon the test fixture deletion.
        /// </summary>
        internal OnTeardownNoSqlContainer Items { get; private set; }

        /// <summary>
        /// (default for cleaning items) Configures the <see cref="TemporaryNoSqlContainer"/> to only delete the NoSQL items
        /// in an Azure Cosmos DB for NoSQL container upon disposal if the item was upserted by the test fixture (using <see cref="TemporaryNoSqlContainer.AddItemAsync{TItem}"/>).
        /// </summary>
        [Obsolete("Will be removed in v3, please use " + nameof(CleanUpsertedItems) + " instead that provides exactly the same on-teardown functionality")]
        public OnTeardownNoSqlContainerOptions CleanCreatedItems()
        {
            return CleanUpsertedItems();
        }

        /// <summary>
        /// (default for cleaning items) Configures the <see cref="TemporaryNoSqlContainer"/> to only delete or revert the NoSQL items
        /// in an Azure Cosmos DB for NoSQL container upon disposal if the item was upserted by the test fixture (using <see cref="TemporaryNoSqlContainer.UpsertItemAsync{TItem}"/>).
        /// </summary>
        public OnTeardownNoSqlContainerOptions CleanUpsertedItems()
        {
            Items = OnTeardownNoSqlContainer.CleanIfUpserted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete all the NoSQL items
        /// in an Azure Cosmos DB for NoSQL container upon disposal - even if the test fixture didn't add them.
        /// </summary>
        public OnTeardownNoSqlContainerOptions CleanAllItems()
        {
            Items = OnTeardownNoSqlContainer.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSQL items
        /// in an Azure Cosmos DB for NoSQL container upon disposal that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///   <para>Multiple calls will be aggregated together in an OR expression.</para>
        ///   <para>
        ///     The matching of items only happens on NoSQL items that were created outside the scope of the test fixture.
        ///     All items upserted by the test fixture will be deleted or reverted upon disposal, even if the items do not match one of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        ///   </para>
        /// </remarks>
        /// <param name="filters">The filters  to match NoSQL items that should be removed.</param>
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
        /// Configures the <see cref="TemporaryNoSqlContainer"/> to delete the NoSQL items
        /// in an Azure Cosmos DB for NoSQL container upon disposal that match any of the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///   <para>Multiple calls will be aggregated together in an OR expression.</para>
        ///   <para>
        ///     The matching of items only happens on NoSQL items that were created outside the scope of the test fixture.
        ///     All items upserted by the test fixture will be deleted or reverted upon disposal, even if the items do not match one of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.
        ///   </para>
        /// </remarks>
        /// <typeparam name="TItem">The custom type of the NoSQL item.</typeparam>
        /// <param name="filters">The filters  to match NoSQL items that should be removed.</param>
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
        internal bool IsMatched(string itemId, PartitionKey partitionKey, JsonObject itemStream, CosmosClient client)
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
        public OnTeardownNoSqlContainerOptions OnTeardown { get; } = new OnTeardownNoSqlContainerOptions().CleanUpsertedItems();
    }

    /// <summary>
    /// Represents a temporary Azure Cosmos DB for NoSQL container that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryNoSqlContainer : IAsyncDisposable
    {
        private readonly CosmosDBSqlContainerResource _container;
        private readonly CosmosClient _resourceClient;
        private readonly bool _createdByUs;
        private readonly Collection<IAsyncDisposable> _items = [];
        private readonly TemporaryNoSqlContainerOptions _options;
        private readonly DisposableCollection _disposables;
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
            _disposables = new DisposableCollection(_logger);

            Client = containerClient;
        }

        /// <summary>
        /// Gets the unique name of the NoSQL container, currently available on Azure Cosmos DB.
        /// </summary>
        public string Name => Client.Id;

        /// <summary>
        /// Gets the client to interact with the Azure Cosmos DB for NoSQL container.
        /// </summary>
        public Container Client { get; }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryNoSqlContainer"/>.
        /// </summary>
        public OnTeardownNoSqlContainerOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos DB NoSQL container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos DB resource where the temporary NoSQL container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="databaseName">The name of the existing NoSQL database in the Azure Cosmos DB resource.</param>
        /// <param name="containerName">The name of the NoSQL container to be created within the Azure Cosmos DB resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSQL item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSQL container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbAccountResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger)
        {
            return CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos DB for NoSql container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos DB resource where the temporary NoSQL container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="databaseName">The name of the existing NoSQL database in the Azure Cosmos resource.</param>
        /// <param name="containerName">The name of the NoSQL container to be created within the Azure Cosmos DB resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSQL item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSQL container.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cosmosDbAccountResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger,
            Action<TemporaryNoSqlContainerOptions> configureOptions)
        {
            return CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                new DefaultAzureCredential(),
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos DB for NoSQL container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos DB resource where the temporary NoSQL container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="credential">The credential implementation to authenticate with the Azure Cosmos DB resource.</param>
        /// <param name="databaseName">The name of the existing NoSQL database in the Azure Cosmos DB resource.</param>
        /// <param name="containerName">The name of the NoSQL container to be created within the Azure Cosmos DB resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSql item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSQL container.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="cosmosDbAccountResourceId"/> or the <paramref name="credential"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="databaseName"/>, <paramref name="containerName"/>, or the <paramref name="partitionKeyPath"/> is blank.
        /// </exception>
        public static Task<TemporaryNoSqlContainer> CreateIfNotExistsAsync(
            ResourceIdentifier cosmosDbAccountResourceId,
            TokenCredential credential,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            ILogger logger)
        {
            return CreateIfNotExistsAsync(
                cosmosDbAccountResourceId,
                credential,
                databaseName,
                containerName,
                partitionKeyPath,
                logger,
                configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryNoSqlContainer"/> which creates a new Azure Cosmos DB for NoSQL container if it doesn't exist yet.
        /// </summary>
        /// <param name="cosmosDbAccountResourceId">
        ///   <para>The resource ID of the Azure Cosmos DB resource where the temporary NoSQL container should be created.</para>
        ///   <para>The resource ID can be constructed with the <see cref="CosmosDBAccountResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier cosmosDbAccountResourceId =
        ///           CosmosDBAccountResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;account-name&gt;");
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="credential">The credential implementation to authenticate with the Azure Cosmos DB resource.</param>
        /// <param name="databaseName">The name of the existing NoSQL database in the Azure Cosmos DB resource.</param>
        /// <param name="containerName">The name of the NoSQL container to be created within the Azure Cosmos DB resource.</param>
        /// <param name="partitionKeyPath">The path to the partition key of the NoSQL item which describes how the items should be partitioned.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the lifetime of the NoSQL container.</param>
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

            CosmosDBAccountResource cosmosDb = await GetCosmosDbResourceAsync(cosmosDbAccountResourceId, credential).ConfigureAwait(false);
            CosmosDBSqlDatabaseResource database = await cosmosDb.GetCosmosDBSqlDatabaseAsync(databaseName).ConfigureAwait(false);

            var cosmosClient = new CosmosClient(cosmosDb.Data.DocumentEndpoint, credential);
            Container containerClient = cosmosClient.GetContainer(databaseName, containerName);

            if (await database.GetCosmosDBSqlContainers().ExistsAsync(containerName).ConfigureAwait(false))
            {
                logger.LogSetupUseExistingContainer(containerName, databaseName);
                await CleanContainerOnSetupAsync(containerClient, options, logger).ConfigureAwait(false);

                CosmosDBSqlContainerResource container = await database.GetCosmosDBSqlContainerAsync(containerName).ConfigureAwait(false);
                return new TemporaryNoSqlContainer(cosmosClient, containerClient, container, createdByUs: false, options, logger);
            }
            else
            {
                logger.LogSetupCreateNewContainer(containerName, databaseName);

                var properties = new ContainerProperties(containerName, partitionKeyPath);
                CosmosDBSqlContainerResource container = await CreateNewNoSqlContainerAsync(cosmosDb, database, properties).ConfigureAwait(false);

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
                          .CreateOrUpdateAsync(WaitUntil.Completed, properties.Id, request)
                          .ConfigureAwait(false);

            return await database.GetCosmosDBSqlContainerAsync(properties.Id).ConfigureAwait(false);
        }

        private static Task<Azure.Response<CosmosDBAccountResource>> GetCosmosDbResourceAsync(ResourceIdentifier cosmosDbAccountResourceId, TokenCredential credential)
        {
            var arm = new ArmClient(credential);
            CosmosDBAccountResource cosmosDb = arm.GetCosmosDBAccountResource(cosmosDbAccountResourceId);

            return cosmosDb.GetAsync();
        }

        private static Task CleanContainerOnSetupAsync(Container container, TemporaryNoSqlContainerOptions options, ILogger logger)
        {
            if (options.OnSetup.Items is OnSetupNoSqlContainer.LeaveExisted)
            {
                return Task.CompletedTask;
            }

            if (options.OnSetup.Items is OnSetupNoSqlContainer.CleanIfExisted)
            {
                return ForEachItemAsync(container, async (id, partitionKey, _) =>
                {
                    logger.LogSetupDeleteItem(id, partitionKey, container.Database.Id, container.Id);
                    using ResponseMessage response = await container.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw new RequestFailedException((int) response.StatusCode,
                            $"[Test:Setup] Failed to delete NoSQL item '{id}' {partitionKey} in Azure Cosmos DB for NoSQL container '{container.Database.Id}/{container.Id}' " +
                            $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                    }
                });
            }

            if (options.OnSetup.Items is OnSetupNoSqlContainer.CleanIfMatched)
            {
                return ForEachItemAsync(container, async (id, partitionKey, doc) =>
                {
                    if (options.OnSetup.IsMatched(id, partitionKey, doc, container.Database.Client))
                    {
                        logger.LogSetupDeleteMatchedItem(id, partitionKey, container.Database.Id, container.Id);
                        using ResponseMessage response = await container.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                        {
                            throw new RequestFailedException((int) response.StatusCode,
                                $"[Test:Setup] Failed to delete matched NoSQL item '{id}' {partitionKey} in Azure Cosmos DB for NoSQL container '{container.Database.Id}/{container.Id}' " +
                                $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                        }
                    }
                });
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a temporary NoSql item to the current container instance.
        /// </summary>
        /// <typeparam name="T">The custom NoSQL model.</typeparam>
        /// <param name="item">The item to create in the NoSQL container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="item"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3, please use the " + nameof(UpsertItemAsync) + "instead that provides exactly the same functionality")]
        public Task AddItemAsync<T>(T item)
        {
            return UpsertItemAsync(item);
        }

        /// <summary>
        /// Adds a new or replaces an existing NoSQL item in the Azure Cosmos DB for NoSQL container (a.k.a. UPSERT).
        /// </summary>
        /// <remarks>
        ///     ⚡ Any items upserted via this call will always be deleted (if new) or reverted (if existing)
        ///     when the <see cref="TemporaryNoSqlContainer"/> is disposed.
        /// </remarks>
        /// <param name="item">The item to create in the Azure Cosmos DB for NoSQL container.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="item"/> is <c>null</c>.</exception>
        public async Task UpsertItemAsync<TItem>(TItem item)
        {
            ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
            ArgumentNullException.ThrowIfNull(item);
            _items.Add(await TemporaryNoSqlItem.UpsertItemAsync(Client, item, _logger).ConfigureAwait(false));
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
                _disposables.AddRange(_items);

                if (_createdByUs)
                {
                    _disposables.Add(AsyncDisposable.Create(() =>
                    {
                        _logger.LogTeardownDeleteContainer(_container.Id.Name, _container.Id.Parent?.Name ?? "<not-available>");
                        return _container.DeleteAsync(WaitUntil.Completed);
                    }));
                }
                else
                {
                    await CleanContainerOnTeardownAsync(_disposables).ConfigureAwait(false);
                }

                _disposables.Add(_resourceClient);

                GC.SuppressFinalize(this);
            }
        }

        private async Task CleanContainerOnTeardownAsync(DisposableCollection disposables)
        {
            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfUpserted)
            {
                return;
            }

            if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanAll)
            {
                await ForEachItemAsync((id, partitionKey, _) =>
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTeardownDeleteItem(id, partitionKey, Client.Database.Id, Client.Id);
                        using ResponseMessage response = await Client.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                        {
                            throw new RequestFailedException((int) response.StatusCode,
                                $"[Test:Teardown] Failed to delete NoSQL item '{id}' {partitionKey} in Azure Cosmos DB for NoSQL container '{Client.Database.Id}/{Client.Id}' " +
                                $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                        }
                    }));

                    return Task.CompletedTask;

                }).ConfigureAwait(false);
            }
            else if (_options.OnTeardown.Items is OnTeardownNoSqlContainer.CleanIfMatched)
            {
                await ForEachItemAsync((id, partitionKey, doc) =>
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        if (_options.OnTeardown.IsMatched(id, partitionKey, doc, Client.Database.Client))
                        {
                            _logger.LogTeardownDeleteMatchedItem(id, partitionKey, Client.Database.Id, Client.Id);
                            using ResponseMessage response = await Client.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false);

                            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                            {
                                throw new RequestFailedException((int) response.StatusCode,
                                    $"[Test:Teardown] Failed to delete matched NoSQL item '{id}' {partitionKey} in Azure Cosmos DB for NoSQL container '{Client.Database.Id}/{Client.Id}' " +
                                    $"since the delete operation responded with a failure: {(int) response.StatusCode} {response.StatusCode}: {response.ErrorMessage}");
                            }
                        }
                    }));

                    return Task.CompletedTask;

                }).ConfigureAwait(false);
            }
        }

        private Task ForEachItemAsync(Func<string, PartitionKey, JsonObject, Task> deleteItemAsync)
        {
            return ForEachItemAsync(Client, deleteItemAsync);
        }

        private static async Task ForEachItemAsync(Container container, Func<string, PartitionKey, JsonObject, Task> deleteItemAsync)
        {
            ContainerResponse resp = await container.ReadContainerAsync().ConfigureAwait(false);
            ContainerProperties properties = resp.Resource;

            using FeedIterator iterator = container.GetItemQueryStreamIterator();
            while (iterator.HasMoreResults)
            {
                using ResponseMessage message = await iterator.ReadNextAsync().ConfigureAwait(false);
                if (!message.IsSuccessStatusCode)
                {
                    continue;
                }

                JsonNode json = await JsonNode.ParseAsync(message.Content, DeserializeOptions).ConfigureAwait(false);

                if (json is not JsonObject root
                    || !root.TryGetPropertyValue("Documents", out JsonNode docs)
                    || docs is not JsonArray docsArr)
                {
                    continue;
                }

                foreach (JsonObject doc in docsArr.OfType<JsonObject>().ToArray())
                {
                    string id = ExtractIdFromItem(doc);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    PartitionKey partitionKey = ExtractPartitionKeyFromItem(doc, properties);
                    await deleteItemAsync(id, partitionKey, doc).ConfigureAwait(false);
                }
            }
        }
    }

    internal static partial class TempNoSqlContainerILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Create new Azure Cosmos DB for NoSQL container '{ContainerName}' in database '{DatabaseName}'")]
        internal static partial void LogSetupCreateNewContainer(this ILogger logger, string containerName, string databaseName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Use already existing Azure Cosmos DB for NoSQL container '{ContainerName}' in database '{DatabaseName}'")]
        internal static partial void LogSetupUseExistingContainer(this ILogger logger, string containerName, string databaseName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Delete NoSQL item '{ItemId}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogSetupDeleteItem(this ILogger logger, string itemId, PartitionKey partitionKey, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Delete matched NoSQL item '{ItemId}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogSetupDeleteMatchedItem(this ILogger logger, string itemId, PartitionKey partitionKey, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete NoSQL item '{ItemId}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogTeardownDeleteItem(this ILogger logger, string itemId, PartitionKey partitionKey, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete matched NoSQL item '{ItemId}' {PartitionKey} in Azure Cosmos DB for NoSQL container '{DatabaseName}/{ContainerName}'")]
        internal static partial void LogTeardownDeleteMatchedItem(this ILogger logger, string itemId, PartitionKey partitionKey, string databaseName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Cosmos DB for NoSQL container '{ContainerName}' in database '{DatabaseName}'")]
        internal static partial void LogTeardownDeleteContainer(this ILogger logger, string containerName, string databaseName);
    }
}
