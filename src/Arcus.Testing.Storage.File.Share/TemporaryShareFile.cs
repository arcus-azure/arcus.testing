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
        private readonly ILogger _logger;

        private TemporaryShareFile(
            ShareFileClient client,
            (Stream stream, long length) original,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);

            _original = original;
            _logger = logger ?? NullLogger.Instance;

            Client = client;
        }

        /// <summary>
        /// Gets the client to interact with the temporary stored Azure Files share currently in storage.
        /// </summary>
        public ShareFileClient Client { get; }

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
        public static async Task<TemporaryShareFile> UpsertFileAsync(ShareDirectoryClient directoryClient, string fileName, Stream fileContents, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(directoryClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
            return await UpsertFileAsync(fileClient, fileContents, logger);
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

            if (await fileClient.ExistsAsync())
            {
                logger.LogTrace("[Test:Setup] Replace already existing Azure Files share file '{FileName}' in directory '{AccountName}/{FilePath}'", fileClient.Name, fileClient.AccountName, fileClient.Path);

                ShareFileDownloadInfo result = await fileClient.DownloadAsync();
                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadAsync(fileStream);

                return new TemporaryShareFile(fileClient, (result.Content, result.ContentLength), logger);
            }

            try
            {
                logger.LogTrace("[Test:Setup] Upload Azure Files share file '{FileName}' in directory '{AccountName}/{FilePath}'", fileClient.Name, fileClient.AccountName, fileClient.Path);
                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadAsync(fileStream);
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
            await using var disposables = new DisposableCollection(_logger);

            if (_original.stream is null)
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("[Test:Teardown] Delete Azure Files share file '{FileName}' in directory '{AccountName}/{DirectoryName}'", Client.Name, Client.AccountName, Client.Path);
                    await Client.DeleteIfExistsAsync();
                }));
            }
            else
            {
                disposables.Add(AsyncDisposable.Create(async () =>
                {
                    _logger.LogTrace("[Test:Teardown] Replace Azure Files share file '{FileName}' with original contents in directory '{AccountName}/{DirectoryName}'", Client.Name, Client.AccountName, Client.Path);
                    await Client.CreateAsync(_original.length);
                    await Client.UploadAsync(_original.stream);

                    await _original.stream.DisposeAsync();
                }));
            }

            GC.SuppressFinalize(this);
        }
    }
}
