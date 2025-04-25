using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    internal enum OnSetupDirectoryShare { LeaveExisted = 0, CleanIfExisted, CleanIfMatched }

    internal enum OnTeardownDirectoryShare { CleanIfCreated = 0, CleanAll, CleanIfMatched }

    /// <summary>
    /// Represents the available options when creating a <see cref="TemporaryShareDirectory"/>.
    /// </summary>
    public class OnSetupTemporaryShareDirectoryOptions
    {
        private readonly List<Func<ShareFileItem, bool>> _filters = [];

        internal OnSetupDirectoryShare Items { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryShareDirectory"/> to leave all the items (both files and directories) in the directory share untouched
        /// that already existed before the creation of the <see cref="TemporaryShareDirectory"/>.
        /// </summary>
        public OnSetupTemporaryShareDirectoryOptions LeaveAllItems()
        {
            Items = OnSetupDirectoryShare.LeaveExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryShareDirectory"/> to clean all the items (both files and directories) in the directory share
        /// that already existed before the creation of the <see cref="TemporaryShareDirectory"/>.
        /// </summary>
        public OnSetupTemporaryShareDirectoryOptions CleanAllItems()
        {
            Items = OnSetupDirectoryShare.CleanIfExisted;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryShareDirectory"/> to clean all the items (both files and directories) in the directory share
        /// upon the creation of the test fixture that matches one of the provided <paramref name="filters"/>.
        /// </summary>
        /// <param name="filters">The filters to match the stored items in the directory share.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains a <c>null</c> element.</exception>
        public OnSetupTemporaryShareDirectoryOptions CleanMatchingItems(params Func<ShareFileItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all provided Azure File share item filters to be non-null", nameof(filters));
            }

            _filters.AddRange(filters);
            Items = OnSetupDirectoryShare.CleanIfMatched;

            return this;
        }

        internal bool IsMatch(ShareFileItem item)
        {
            return _filters.Exists(f => f(item));
        }
    }

    /// <summary>
    /// Represents the available options when deleting a <see cref="TemporaryShareDirectory"/>.
    /// </summary>
    public class OnTeardownTemporaryShareDirectoryOptions
    {
        private readonly List<Func<ShareFileItem, bool>> _filters = [];

        internal OnTeardownDirectoryShare Items { get; private set; }

        /// <summary>
        /// (default) Configures the <see cref="TemporaryShareDirectory"/> to clean only the items (both files and directories) in the directory share
        /// that the test fixture was responsible for creating, upon the deletion of the fixture.
        /// </summary>
        public OnTeardownTemporaryShareDirectoryOptions CleanCreatedItems()
        {
            Items = OnTeardownDirectoryShare.CleanIfCreated;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryShareDirectory"/> to clean all the items (both files and directories) in the directory share
        /// upon the deletion of the test fixture.
        /// </summary>
        public OnTeardownTemporaryShareDirectoryOptions CleanAllItems()
        {
            Items = OnTeardownDirectoryShare.CleanAll;
            return this;
        }

        /// <summary>
        /// Configures the <see cref="TemporaryShareDirectory"/> to clean all the items (both files and directories) in the directory share
        /// upon the deletion of the test fixture that matches one of the provided <paramref name="filters"/>.
        /// </summary>
        /// <param name="filters">The filters to match the stored items in the directory share.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="filters"/> contains a <c>null</c> element.</exception>
        public OnTeardownTemporaryShareDirectoryOptions CleanMatchingItems(params Func<ShareFileItem, bool>[] filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            if (Array.Exists(filters, f => f is null))
            {
                throw new ArgumentException("Requires all provided Azure File share item filters to be non-null", nameof(filters));
            }

            _filters.AddRange(filters);
            Items = OnTeardownDirectoryShare.CleanIfMatched;

            return this;
        }

        internal bool IsMatch(ShareFileItem item)
        {
            return _filters.Exists(filter => filter(item));
        }
    }

    /// <summary>
    /// Represents the available options to manipulate the behavior of the <see cref="TemporaryShareDirectory"/>.
    /// </summary>
    public class TemporaryShareDirectoryOptions
    {
        /// <summary>
        /// Gets the options to manipulate the creation of the <see cref="TemporaryShareDirectory"/>.
        /// </summary>
        public OnSetupTemporaryShareDirectoryOptions OnSetup { get; } = new OnSetupTemporaryShareDirectoryOptions().LeaveAllItems();

        /// <summary>
        /// Gets the options to manipulate the deletion of the <see cref="TemporaryShareDirectory"/>.
        /// </summary>
        public OnTeardownTemporaryShareDirectoryOptions OnTeardown { get; } = new OnTeardownTemporaryShareDirectoryOptions().CleanCreatedItems();
    }

    /// <summary>
    /// Represents a temporary directory share on an Azure Storage file share.
    /// </summary>
    public class TemporaryShareDirectory : IAsyncDisposable
    {
        private readonly bool _createdByUs;
        private readonly Collection<TemporaryShareFile> _files = [];
        private readonly TemporaryShareDirectoryOptions _options;
        private readonly ILogger _logger;

        private TemporaryShareDirectory(
            ShareDirectoryClient client,
            bool createdByUs,
            TemporaryShareDirectoryOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _createdByUs = createdByUs;
            _options = options;
            _logger = logger ?? NullLogger.Instance;

            Client = client;
        }

        /// <summary>
        /// Represents the client to interact with the temporary stored Azure Storage file share currently in storage.
        /// </summary>
        public ShareDirectoryClient Client { get; }

        /// <summary>
        /// Gets the options to manipulate the deletion of the <see cref="TemporaryShareDirectory"/>.
        /// </summary>
        public OnTeardownTemporaryShareDirectoryOptions OnTeardown => _options.OnTeardown;

        /// <summary>
        /// Creates a temporary directory share on an Azure Storage file share resource.
        /// </summary>
        /// <param name="shareClient">The client to interact with the Azure File share resource.</param>
        /// <param name="directoryName">The name of the directory to create on the file share.</param>
        /// <param name="logger">The logger instance to write diagnostic traces during the lifetime of the fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="shareClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="directoryName"/> is blank.</exception>
        public static async Task<TemporaryShareDirectory> CreateIfNotExistsAsync(ShareClient shareClient, string directoryName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(shareClient, directoryName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a temporary directory share on an Azure Storage file share resource.
        /// </summary>
        /// <param name="shareClient">The client to interact with the Azure File share resource.</param>
        /// <param name="directoryName">The name of the directory to create on the file share.</param>
        /// <param name="logger">The logger instance to write diagnostic traces during the lifetime of the fixture.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="shareClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="directoryName"/> is blank.</exception>
        public static async Task<TemporaryShareDirectory> CreateIfNotExistsAsync(
            ShareClient shareClient,
            string directoryName,
            ILogger logger,
            Action<TemporaryShareDirectoryOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(shareClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);

            ShareDirectoryClient dirClient = shareClient.GetDirectoryClient(directoryName);
            return await CreateIfNotExistsAsync(dirClient, logger, configureOptions);
        }

        /// <summary>
        /// Creates a temporary directory share on an Azure Storage file share resource.
        /// </summary>
        /// <param name="directoryClient">The client to interact with the directory share in the Azure File share resource.</param>
        /// <param name="logger">The logger instance to write diagnostic traces during the lifetime of the fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="directoryClient"/> is <c>null</c>.</exception>
        public static async Task<TemporaryShareDirectory> CreateIfNotExistsAsync(ShareDirectoryClient directoryClient, ILogger logger)
        {
            return await CreateIfNotExistsAsync(directoryClient, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a temporary directory share on an Azure Storage file share resource.
        /// </summary>
        /// <param name="directoryClient">The client to interact with the directory share in the Azure File share resource.</param>
        /// <param name="logger">The logger instance to write diagnostic traces during the lifetime of the fixture.</param>
        /// <param name="configureOptions">The additional options to manipulate the behavior of the test fixture during its lifetime.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="directoryClient"/> is <c>null</c>.</exception>
        public static async Task<TemporaryShareDirectory> CreateIfNotExistsAsync(
            ShareDirectoryClient directoryClient,
            ILogger logger,
            Action<TemporaryShareDirectoryOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(directoryClient);
            logger ??= NullLogger.Instance;

            var options = new TemporaryShareDirectoryOptions();
            configureOptions?.Invoke(options);

            if (await directoryClient.ExistsAsync())
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure File share directory '{DirectoryName}' at '{AccountName}/{DirectoryPath}'", directoryClient.Name, directoryClient.AccountName, directoryClient.Path);
                await CleanDirectoryOnSetupAsync(directoryClient, options, logger);

                return new TemporaryShareDirectory(directoryClient, createdByUs: false, options, logger);
            }

            try
            {
                logger.LogTrace("[Test:Setup] Create new Azure File share directory '{DirectoryName}' at '{AccountName}/{DirectoryPath}'", directoryClient.Name, directoryClient.AccountName, directoryClient.Path);
                await directoryClient.CreateAsync();
            }
            catch (RequestFailedException exception) when (exception.ErrorCode == ShareErrorCode.ShareNotFound)
            {
                throw new DriveNotFoundException(
                    $"[Test:Setup] Cannot create a new Azure File share directory '{directoryClient.Name}' at '{directoryClient.AccountName}/{directoryClient.Path}' " +
                    $"because the share '{directoryClient.ShareName}' does not exists in account '{directoryClient.AccountName}'; " +
                    $"please make sure to use an existing Azure File share to create a temporary directory test fixture",
                    exception);
            }

            return new TemporaryShareDirectory(directoryClient, createdByUs: true, options, logger);
        }

        private static async Task CleanDirectoryOnSetupAsync(ShareDirectoryClient directoryClient, TemporaryShareDirectoryOptions options, ILogger logger)
        {
            if (options.OnSetup.Items is OnSetupDirectoryShare.LeaveExisted)
            {
                return;
            }

            if (options.OnSetup.Items is OnSetupDirectoryShare.CleanIfExisted)
            {
                await DeleteAllDirectoryContentsAsync(directoryClient, TestFixture.Setup, logger);
            }
            else if (options.OnSetup.Items is OnSetupDirectoryShare.CleanIfMatched)
            {
                await DeleteDirectoryContentsAsync(directoryClient, options.OnSetup.IsMatch, TestFixture.Setup, logger);
            }
        }

        /// <summary>
        /// Creates a new or replaces an existing file in this directory share (a.k.a. UPSERT).
        /// </summary>
        /// <remarks>
        ///     Any files uploaded via this call will always be deleted (if new) or reverted (if existing) when this instance is disposed.
        /// </remarks>
        /// <param name="fileName">The name of the file to upload to the share directory.</param>
        /// <param name="fileContents">The contents of the file to upload to the share directory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="fileContents"/> is <c>null</c>.</exception>
        public async Task UpsertFileAsync(string fileName, Stream fileContents)
        {
            _files.Add(await TemporaryShareFile.UpsertFileAsync(Client, fileName, fileContents, _logger));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);
            disposables.AddRange(_files);

            if (_createdByUs)
            {
                await DeleteAllDirectoryContentsAsync(Client, TestFixture.Teardown, _logger);

                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("[Test:Teardown] Delete Azure File share directory '{DirectoryName}' at '{AccountName}/{DirectoryPath}'", Client.Name, Client.AccountName, Client.Path);
                    await Client.DeleteAsync();
                }));
            }
            else
            {
                await CleanDirectoryOnTeardownAsync();
            }

            GC.SuppressFinalize(this);
        }

        private async Task CleanDirectoryOnTeardownAsync()
        {
            if (_options.OnTeardown.Items is OnTeardownDirectoryShare.CleanIfCreated)
            {
                return;
            }

            if (_options.OnTeardown.Items is OnTeardownDirectoryShare.CleanAll)
            {
                await DeleteAllDirectoryContentsAsync(Client, TestFixture.Teardown, _logger);
            }
            else if (_options.OnTeardown.Items is OnTeardownDirectoryShare.CleanIfMatched)
            {
                await DeleteDirectoryContentsAsync(Client, _options.OnTeardown.IsMatch, TestFixture.Teardown, _logger);
            }
        }

        private enum TestFixture { Setup, Teardown }

        private static async Task DeleteDirectoryContentsAsync(
            ShareDirectoryClient current,
            Func<ShareFileItem, bool> shouldDeleteItem,
            TestFixture state,
            ILogger logger)
        {
            await using var disposables = new DisposableCollection(logger);

            disposables.Add(AsyncDisposable.Create(async () =>
            {
                await current.ForceCloseAllHandlesAsync(recursive: true);

                await foreach (ShareFileItem item in current.GetFilesAndDirectoriesAsync())
                {
                    if (item.IsDirectory)
                    {
                        ShareDirectoryClient sub = current.GetSubdirectoryClient(item.Name);
                        if (shouldDeleteItem(item))
                        {
                            await DeleteAllDirectoryContentsAsync(sub, state, logger);

                            logger.LogTrace("[Test:{State}] Delete Azure File share directory '{DirectoryName}' at '{AccountName}/{DirectoryPath}'", state, sub.Name, sub.AccountName, sub.Path);
                            await sub.DeleteIfExistsAsync();
                        }
                        else
                        {
                            await DeleteDirectoryContentsAsync(sub, shouldDeleteItem, state, logger);
                        }
                    }
                    else if (shouldDeleteItem(item))
                    {
                        ShareFileClient file = current.GetFileClient(item.Name);

                        logger.LogTrace("[Test:{State}] Delete Azure File share item '{FileName}' at '{AccountName}/{FilePath}'", state, file.Name, file.AccountName, file.Path);
                        await file.DeleteIfExistsAsync();
                    }
                }
            }));
        }

        private static async Task DeleteAllDirectoryContentsAsync(ShareDirectoryClient current, TestFixture state, ILogger logger)
        {
            await DeleteDirectoryContentsAsync(current, shouldDeleteItem: _ => true, state, logger);
        }
    }
}
