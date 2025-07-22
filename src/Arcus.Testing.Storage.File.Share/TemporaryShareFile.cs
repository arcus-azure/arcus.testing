using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a file that is temporary available on an Azure Files share directory.
    /// </summary>
    public class TemporaryShareFile : IAsyncDisposable
    {
        private readonly (Stream stream, long length) _original;
        private readonly ShareFileClient _client;
        private readonly DisposableCollection _disposables;
        private readonly ILogger _logger;

        private TemporaryShareFile(
            ShareFileClient client,
            (Stream stream, long length) original,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _original = original;
            _logger = logger ?? NullLogger.Instance;
            _disposables = new DisposableCollection(_logger);

            _client = client;
        }

        /// <summary>
        /// Gets the client to interact with the temporary stored Azure Files share currently in storage.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the test fixture was already teared down.</exception>
        public ShareFileClient Client
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposables.IsDisposed, this);
                return _client;
            }
        }

        /// <summary>
        /// Creates a new or replaces an existing file on an Azure Files share directory.
        /// </summary>
        /// <remarks>
        ///     Make sure that the <paramref name="fileContents"/>'s <see cref="Stream.Length"/> is accessible,
        ///     so the appropriate size can be set on the file share.
        /// </remarks>
        /// <param name="directoryClient">The client to interact with the share directory to upload to file to.</param>
        /// <param name="fileName">The name of the file to upload to the share directory.</param>
        /// <param name="fileContents">The contents of the file to upload to the share directory.</param>
        /// <param name="logger">The instance to log diagnostic traces during the lifetime of the fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="directoryClient"/> or the <paramref name="fileContents"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        public static Task<TemporaryShareFile> UpsertFileAsync(ShareDirectoryClient directoryClient, string fileName, Stream fileContents, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(directoryClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
            return UpsertFileAsync(fileClient, fileContents, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing file on an Azure Files share directory.
        /// </summary>
        /// <remarks>
        ///     Make sure that the <paramref name="fileStream"/>'s <see cref="Stream.Length"/> is accessible,
        ///     so the appropriate size can be set on the file share.
        /// </remarks>
        /// <param name="fileClient">The client to interact with the share file to upload to file to.</param>
        /// <param name="fileStream">The contents of the file to upload to the share directory.</param>
        /// <param name="logger">The instance to log diagnostic traces during the lifetime of the fixture.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="fileClient"/> or the <paramref name="fileStream"/> is <c>null</c>.</exception>
        public static async Task<TemporaryShareFile> UpsertFileAsync(ShareFileClient fileClient, Stream fileStream, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(fileClient);
            ArgumentNullException.ThrowIfNull(fileStream);
            logger ??= NullLogger.Instance;

            if (await fileClient.ExistsAsync().ConfigureAwait(false))
            {
                logger.LogSetupReplaceExistingFile(fileClient.Name, fileClient.AccountName, fileClient.Path);

                ShareFileDownloadInfo result = await fileClient.DownloadAsync().ConfigureAwait(false);
                await fileClient.CreateAsync(fileStream.Length).ConfigureAwait(false);
                await fileClient.UploadAsync(fileStream).ConfigureAwait(false);

                return new TemporaryShareFile(fileClient, (result.Content, result.ContentLength), logger);
            }

            try
            {
                logger.LogSetupUploadNewFile(fileClient.Name, fileClient.AccountName, fileClient.Path);

                await fileClient.CreateAsync(fileStream.Length).ConfigureAwait(false);
                await fileClient.UploadAsync(fileStream).ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when (exception.ErrorCode == ShareErrorCode.ShareNotFound)
            {
                throw new DriveNotFoundException(
                    $"[Test:Setup] Cannot upload a new Azure Files share file '{fileClient.Name}' at '{fileClient.AccountName}/{fileClient.Path}' " +
                    $"because the share '{fileClient.ShareName}' does not exists in account '{fileClient.AccountName}'; " +
                    $"please make sure to use an existing Azure Files share to create a temporary file test fixture",
                    exception);
            }
            catch (RequestFailedException exception) when (exception.ErrorCode == ShareErrorCode.ParentNotFound)
            {
                throw new DirectoryNotFoundException(
                    $"[Test:Setup] Cannot upload a new Azure Files share file '{fileClient.Name}' at '{fileClient.AccountName}/{fileClient.Path}' " +
                    $"because the parent directory does not exists in account '{fileClient.AccountName}'; " +
                    $"please make sure to use an existing Azure Files share directory to create a temporary file test fixture",
                    exception);
            }

            return new TemporaryShareFile(fileClient, original: (null, length: -1), logger);
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
                if (_original.stream is null)
                {
                    _disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTeardownDeleteFile(_client.Name, _client.AccountName, _client.Path);
                        await _client.DeleteIfExistsAsync().ConfigureAwait(false);
                    }));
                }
                else
                {
                    _disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogTeardownRevertFile(_client.Name, _client.AccountName, _client.Path);

                        await _client.CreateAsync(_original.length).ConfigureAwait(false);
                        await _client.UploadAsync(_original.stream).ConfigureAwait(false);

                        await _original.stream.DisposeAsync().ConfigureAwait(false);
                    }));
                }
            }

            GC.SuppressFinalize(this);
        }
    }

    internal static partial class TempShareFileILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Upload new Azure Files share file '{FileName}' at '{AccountName}/{FilePath}'")]
        internal static partial void LogSetupUploadNewFile(this ILogger logger, string fileName, string accountName, string filePath);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Replace already existing Azure Files share file '{FileName}' at '{AccountName}/{FilePath}'")]
        internal static partial void LogSetupReplaceExistingFile(this ILogger logger, string fileName, string accountName, string filePath);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Files share file '{FileName}' at '{AccountName}/{FilePath}'")]
        internal static partial void LogTeardownDeleteFile(this ILogger logger, string fileName, string accountName, string filePath);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Replace Azure Files share file '{FileName}' with original contents at '{AccountName}/{FilePath}'")]
        internal static partial void LogTeardownRevertFile(this ILogger logger, string fileName, string accountName, string filePath);
    }
}
