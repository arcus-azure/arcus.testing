using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when the <see cref="TemporaryBlobContainer"/> is created.
    /// </summary>
    internal enum OnSetupContainer { LeaveExisted = 0, CleanIfExisted = 1, CleanIfMatched = 2 }

    /// <summary>
    /// Represents the available options when the <see cref="TemporaryBlobContainer"/> is cleaned.
    /// </summary>
    internal enum OnTeardownBlobs { CleanIfUpserted = 0, CleanAll = 1, CleanIfMatched = 2 }

    /// <summary>
    /// Represents the available options when the <see cref="TemporaryBlobContainer"/> is deleted.
    /// </summary>
    internal enum OnTeardownContainer { DeleteIfCreated = 0, DeleteIfExists = 1 }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryBlobContainer"/>.
    /// </summary>
    public class OnSetupBlobContainerOptions
    {
        private readonly List<Func<BlobItem, bool>> _filters = [];

        /// <summary>
        /// Gets the configurable setup option on what to do with existing Azure Blobs in the Azure Blob container upon the test fixture creation.
        /// </summary>
        internal OnSetupContainer Blobs { get; private set; } = OnSetupContainer.LeaveExisted;

        /// <summary>
        /// Configures the <see cref="TemporaryBlobContainer"/> to delete all the Azure Blobs upon the test fixture creation.
        /// </summary>
        /// <returns></returns>
        public OnSetupBlobContainerOptions CleanAllBlobs()
        {
            Blobs = OnSetupContainer.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobContainer"/> to delete the Azure Blobs upon the test fixture creation that matched the configured <paramref name="filters"/>.
        /// </summary>
        /// <param name="filters">The filters to match the blobs in the Azure Blob container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when any of the <paramref name="filters"/> is <c>null</c>.</exception>>
        public OnSetupBlobContainerOptions CleanMatchingBlobs(params Func<BlobItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);
            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Blobs = OnSetupContainer.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryBlobContainer"/> to leave all Azure Blobs untouched
        /// that already existed upon the test fixture creation, when there was already an Azure Blob container available.
        /// </summary>
        public OnSetupBlobContainerOptions LeaveAllBlobs()
        {
            Blobs = OnSetupContainer.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Determines whether the given <paramref name="blob"/> matches the configured filter.
        /// </summary>
        /// <param name="blob">The blob to match the filter against.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blob"/> is <c>null</c>.</exception>
        internal bool IsMatched(BlobItem blob)
        {
            ArgumentNullException.ThrowIfNull(blob);

            return Blobs switch
            {
                OnSetupContainer.LeaveExisted => false,
                OnSetupContainer.CleanIfExisted => true,
                OnSetupContainer.CleanIfMatched => _filters.Exists(filter => filter(blob)),
                _ => false
            };
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryBlobContainer"/>.
    /// </summary>
    public class OnTeardownBlobContainerOptions
    {
        private readonly List<Func<BlobItem, bool>> _filters = [];

        /// <summary>
        /// Gets the configurable option on what to do with unlinked Azure Blobs in the Azure Blob container upon the disposal of the test fixture.
        /// </summary>
        internal OnTeardownBlobs Blobs { get; private set; } = OnTeardownBlobs.CleanIfUpserted;

        /// <summary>
        /// Gets the configurable option on what to do with the Azure Blob container upon the disposal of the test fixture.
        /// </summary>
        internal OnTeardownContainer Container { get; private set; } = OnTeardownContainer.DeleteIfCreated;

        /// <summary>
        /// (default for cleaning blobs) Configures the <see cref="TemporaryBlobContainer"/> to only delete the Azure Blobs upon disposal
        /// if the blob was upserted by the test fixture (using <see cref="TemporaryBlobContainer.UpsertBlobFileAsync"/>).
        /// </summary>
        [Obsolete("Will be removed in v3, please use " + nameof(CleanUpsertedBlobs) + " instead that provides exactly the same on-teardown functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public OnTeardownBlobContainerOptions CleanCreatedBlobs()
        {
            return CleanUpsertedBlobs();
        }

        /// <summary>
        /// (default for cleaning blobs) Configures the <see cref="TemporaryBlobContainer"/> to only delete the Azure Blobs upon disposal
        /// if the blob was upserted by the test fixture (using <see cref="TemporaryBlobContainer.UpsertBlobFileAsync"/>).
        /// </summary>
        public OnTeardownBlobContainerOptions CleanUpsertedBlobs()
        {
            Blobs = OnTeardownBlobs.CleanIfUpserted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobContainer"/> to delete all the blobs upon disposal - even if the test fixture didn't upload them.
        /// </summary>
        public OnTeardownBlobContainerOptions CleanAllBlobs()
        {
            Blobs = OnTeardownBlobs.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobContainer"/> to delete the blobs upon disposal that matched the configured <paramref name="filters"/>.
        /// </summary>
        /// <remarks>
        ///     <para>⚡ The matching of blobs only happens on Azure Blobs instances that were created outside the scope of the test fixture.</para>
        ///     <para>⚡ All Blobs created by the test fixture will be deleted upon disposal, regardless of the filters.
        ///     This follows the 'clean environment' principle where the test fixture should clean up after itself and not linger around any state it created.</para>
        /// </remarks>
        /// <param name="filters">The filters to match the blobs in the Azure Blob container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when any of the <paramref name="filters"/> is <c>null</c>.</exception>
        public OnTeardownBlobContainerOptions CleanMatchingBlobs(params Func<BlobItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all filters to be non-null", nameof(filters));
            }

            Blobs = OnTeardownBlobs.CleanIfMatched;
            _filters.AddRange(filters);

            return this;
        }

        /// <summary>
        /// (default for deleting container) Configures the <see cref="TemporaryBlobContainer"/> to only delete the Azure Blob container upon disposal if the test fixture created the container.
        /// </summary>
        public OnTeardownBlobContainerOptions DeleteCreatedContainer()
        {
            Container = OnTeardownContainer.DeleteIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryBlobContainer"/> to delete the Azure Blob container upon disposal, even if it already existed previously - outside the fixture's scope.
        /// </summary>
        public OnTeardownBlobContainerOptions DeleteExistingContainer()
        {
            Container = OnTeardownContainer.DeleteIfExists;
            return this;
        }

        /// <summary>
        /// Determines whether the given <paramref name="blob"/> should be deleted upon the disposal of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        /// <param name="blob">The blob to match the filter against.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blob"/> is <c>null</c>.</exception>
        internal bool IsMatched(BlobItem blob)
        {
            ArgumentNullException.ThrowIfNull(blob);

            return Blobs switch
            {
                OnTeardownBlobs.CleanAll => true,
                OnTeardownBlobs.CleanIfMatched => _filters.Exists(filter => filter(blob)),
                _ => false
            };
        }
    }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryBlobContainer"/>.
    /// </summary>
    public class TemporaryBlobContainerOptions
    {
        /// <summary>
        /// Gets the additional options to manipulate the creation of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        public OnSetupBlobContainerOptions OnSetup { get; } = new OnSetupBlobContainerOptions().LeaveAllBlobs();

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        public OnTeardownBlobContainerOptions OnTeardown { get; } = new OnTeardownBlobContainerOptions().CleanUpsertedBlobs().DeleteCreatedContainer();
    }

    /// <summary>
    /// Represents a temporary Azure Blob container that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryBlobContainer : IAsyncDisposable
    {
        private readonly BlobContainerClient _containerClient;
        private readonly bool _createdByUs;

        private readonly Collection<TemporaryBlobFile> _blobs = [];
        private readonly TemporaryBlobContainerOptions _options;
        private readonly DisposableCollection _disposables;
        private readonly ILogger _logger;

        private TemporaryBlobContainer(
            BlobContainerClient containerClient,
            bool createdByUs,
            TemporaryBlobContainerOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(containerClient);
            ArgumentNullException.ThrowIfNull(options);

            _containerClient = containerClient;
            _createdByUs = createdByUs;
            _options = options;
            _logger = logger ?? NullLogger.Instance;
            _disposables = new DisposableCollection(_logger);
        }

        /// <summary>
        /// Gets the name of the temporary Azure Blob container currently in storage.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public string Name => Client.Name;

        /// <summary>
        /// Gets the <see cref="BlobContainerClient"/> instance that represents the temporary Azure Blob container.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public BlobContainerClient Client
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
                return _containerClient;
            }
        }

        /// <summary>
        /// Gets the additional options to manipulate the deletion of the <see cref="TemporaryBlobContainer"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public OnTeardownBlobContainerOptions OnTeardown
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
                return _options.OnTeardown;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob Storage container if it doesn't exist yet.
        /// </summary>
        /// <remarks>
        ///     <para>⚡ Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob Storage.</para>
        /// </remarks>
        /// <param name="accountName">The name of the Azure Storage account to create the temporary Azure Blob container in.</param>
        /// <param name="containerName">The name of the Azure Blob container to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Blob container.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="containerName"/> is blank.</exception>
        public static Task<TemporaryBlobContainer> CreateIfNotExistsAsync(string accountName, string containerName, ILogger logger)
        {
            return CreateIfNotExistsAsync(accountName, containerName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob Storage container if it doesn't exist yet.
        /// </summary>
        /// <remarks>
        ///     <para>⚡ Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob Storage.</para>
        /// </remarks>
        /// <param name="accountName">The name of the Azure Storage account to create the temporary Azure Blob container in.</param>
        /// <param name="containerName">The name of the Azure Blob container to create.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Blob container.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="accountName"/> or <paramref name="containerName"/> is blank.</exception>
        public static Task<TemporaryBlobContainer> CreateIfNotExistsAsync(
            string accountName,
            string containerName,
            ILogger logger,
            Action<TemporaryBlobContainerOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
            ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

            var blobContainerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
            var containerClient = new BlobContainerClient(blobContainerUri, new DefaultAzureCredential());

            return CreateIfNotExistsAsync(containerClient, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob Storage container if it doesn't exist yet.
        /// </summary>
        /// <param name="containerClient">The client to interact with the Azure Blob Storage container.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the Azure Blob container.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="containerClient"/> is <c>null</c>.</exception>
        public static Task<TemporaryBlobContainer> CreateIfNotExistsAsync(BlobContainerClient containerClient, ILogger logger)
        {
            return CreateIfNotExistsAsync(containerClient, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryBlobContainer"/> which creates a new Azure Blob Storage container if it doesn't exist yet.
        /// </summary>
        /// <param name="containerClient">The client to interact with the Azure Blob Storage container.</param>
        /// <param name="logger">The logger to write diagnostic messages during the creation of the Azure Blob container.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="containerClient"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobContainer> CreateIfNotExistsAsync(
            BlobContainerClient containerClient,
            ILogger logger,
            Action<TemporaryBlobContainerOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(containerClient);
            logger ??= NullLogger.Instance;

            var options = new TemporaryBlobContainerOptions();
            configureOptions?.Invoke(options);

            bool createdByUs = await EnsureContainerCreatedAsync(containerClient, logger).ConfigureAwait(false);
            await CleanBlobContainerUponCreationAsync(containerClient, options, logger).ConfigureAwait(false);

            return new TemporaryBlobContainer(containerClient, createdByUs, options, logger);
        }

        private static async Task<bool> EnsureContainerCreatedAsync(BlobContainerClient containerClient, ILogger logger)
        {
            bool createdByUs = false;
            if (!await containerClient.ExistsAsync().ConfigureAwait(false))
            {
                logger.LogSetupCreateNewContainer(containerClient.Name, containerClient.AccountName);
                await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
                createdByUs = true;
            }
            else
            {
                logger.LogSetupUseExistingContainer(containerClient.Name, containerClient.AccountName);
            }

            return createdByUs;
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobContent"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3, please use the " + nameof(UpsertBlobFileAsync) + "instead that provides exactly the same functionality", DiagnosticId = ObsoleteDefaults.DiagnosticId)]
        public Task<BlobClient> UploadBlobAsync(string blobName, BinaryData blobContent)
        {
            return UpsertBlobFileAsync(blobName, blobContent);
        }

        /// <summary>
        /// Uploads a new or replaces an existing blob in the Azure Blob container (a.k.a. UPSERT).
        /// </summary>
        /// <remarks>
        ///     ⚡ Any blob files upserted via this call will always be deleted (if new) or reverted (if existing)
        ///     when the <see cref="TemporaryBlobContainer"/> is disposed.
        /// </remarks>
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public async Task<BlobClient> UpsertBlobFileAsync(string blobName, BinaryData blobContent)
        {
            ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
            ArgumentNullException.ThrowIfNull(blobContent);

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            _blobs.Add(await TemporaryBlobFile.UpsertFileAsync(blobClient, blobContent, _logger).ConfigureAwait(false));

            return blobClient;
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
                _disposables.AddRange(_blobs);
                _disposables.Add(AsyncDisposable.Create(async () =>
                {
                    await CleanBlobContainerUponDeletionAsync(_containerClient, _options, _logger).ConfigureAwait(false);
                }));

                if (_createdByUs || _options.OnTeardown.Container is OnTeardownContainer.DeleteIfExists)
                {
                    _disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTeardownDeleteContainer(_containerClient.Name, _containerClient.AccountName);
                        await _containerClient.DeleteIfExistsAsync().ConfigureAwait(false);
                    }));
                }
            }

            GC.SuppressFinalize(this);
        }

        private static async Task CleanBlobContainerUponCreationAsync(BlobContainerClient containerClient, TemporaryBlobContainerOptions options, ILogger logger)
        {
            if (options.OnSetup.Blobs is OnSetupContainer.LeaveExisted)
            {
                return;
            }

#pragma warning disable S3267 // Sonar recommends LINQ on loops, but Microsoft has no Async LINQ built-in, besides the additional/outdated `System.Linq.Async` package.
            await foreach (BlobItem blob in containerClient.GetBlobsAsync().ConfigureAwait(false))
#pragma warning restore
            {
                if (options.OnSetup.IsMatched(blob))
                {
                    logger.LogSetupDeleteFile(blob.Name, containerClient.AccountName, containerClient.Name);
                    await containerClient.GetBlobClient(blob.Name).DeleteIfExistsAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task CleanBlobContainerUponDeletionAsync(BlobContainerClient containerClient, TemporaryBlobContainerOptions options, ILogger logger)
        {
            if (options.OnTeardown.Blobs is OnTeardownBlobs.CleanIfUpserted)
            {
                return;
            }

#pragma warning disable S3267 // Sonar recommends LINQ on loops, but Microsoft has no Async LINQ built-in, besides the additional/outdated `System.Linq.Async` package.
            await foreach (BlobItem blob in containerClient.GetBlobsAsync().ConfigureAwait(false))
#pragma warning restore
            {
                if (options.OnTeardown.IsMatched(blob))
                {
                    logger.LogTeardownDeleteFile(blob.Name, containerClient.AccountName, containerClient.Name);
                    await containerClient.GetBlobClient(blob.Name).DeleteIfExistsAsync().ConfigureAwait(false);
                }
            }
        }
    }

    internal static partial class TempBlobContainerILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Create new Azure Blob container '{ContainerName}' in account '{AccountName}'")]
        internal static partial void LogSetupCreateNewContainer(this ILogger logger, string containerName, string accountName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Use already existing Azure Blob container '{ContainerName}' in account '{AccountName}'")]
        internal static partial void LogSetupUseExistingContainer(this ILogger logger, string containerName, string accountName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Delete Azure Blob file '{BlobName}' from container '{AccountName}/{ContainerName}'")]
        internal static partial void LogSetupDeleteFile(this ILogger logger, string blobName, string accountName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Blob container '{ContainerName}' from account '{AccountName}'")]
        internal static partial void LogTeardownDeleteContainer(this ILogger logger, string containerName, string accountName);
    }
}
