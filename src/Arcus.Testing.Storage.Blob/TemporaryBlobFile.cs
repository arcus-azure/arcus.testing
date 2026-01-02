using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Blob file that will be deleted after the instance is disposed.
    /// </summary>
    public class TemporaryBlobFile : IAsyncDisposable
    {
        private readonly bool _createdByUs;
        private readonly BinaryData _originalData;
        private readonly ILogger _logger;

        private TemporaryBlobFile(
            BlobClient blobClient,
            bool createdByUs,
            BinaryData originalData,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(blobClient);

            _createdByUs = createdByUs;
            _originalData = originalData;
            _logger = logger ?? NullLogger.Instance;

            Client = blobClient;
        }

        /// <summary>
        /// Gets the name of the Azure Blob file currently in storage.
        /// </summary>
        public string Name => Client.Name;

        /// <summary>
        /// Gets the name of the Azure Blob container where the Azure Blob file is currently stored.
        /// </summary>
        public string ContainerName => Client.BlobContainerName;

        /// <summary>
        /// Gets the client to interact with the temporary stored Azure Blob file currently in storage.
        /// </summary>
        public BlobClient Client { get; }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob Storage.
        /// </remarks>
        /// <param name="blobContainerUri">
        ///     A <see cref="BlobContainerClient.Uri" /> referencing the blob container that includes the name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> or the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertFileAsync) + " instead which provides the exact same functionality")]
        public static Task<TemporaryBlobFile> UploadIfNotExistsAsync(Uri blobContainerUri, string blobName, BinaryData blobContent, ILogger logger)
        {
            return UpsertFileAsync(blobContainerUri, blobName, blobContent, logger);
        }

        /// <summary>
        /// Uploads a temporary blob to the Azure Blob container.
        /// </summary>
        /// <param name="blobClient">The Azure Blob client to interact with Azure Blob Storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(UpsertFileAsync) + " instead which provides the exact same functionality")]
        public static Task<TemporaryBlobFile> UploadIfNotExistsAsync(BlobClient blobClient, BinaryData blobContent, ILogger logger)
        {
            return UpsertFileAsync(blobClient, blobContent, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing Azure Blob file in an Azure Blob container.
        /// </summary>
        /// <remarks>
        ///     <para>⚡ Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob Storage.</para>
        ///     <para>⚡ File will be deleted (if new) or reverted (if existing) when the <see cref="TemporaryBlobFile"/> is disposed.</para>
        /// </remarks>
        /// <param name="blobClient">The Azure Blob client to interact with Azure Blob Storage.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="blobClient"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static async Task<TemporaryBlobFile> UpsertFileAsync(BlobClient blobClient, BinaryData blobContent, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(blobClient);
            ArgumentNullException.ThrowIfNull(blobContent);
            logger ??= NullLogger.Instance;

            (bool createdByUs, BinaryData originalData) = await EnsureBlobContentCreatedAsync(blobClient, blobContent, logger).ConfigureAwait(false);

            return new TemporaryBlobFile(blobClient, createdByUs, originalData, logger);
        }

        /// <summary>
        /// Creates a new or replaces an existing Azure Blob file in an Azure Blob container.
        /// </summary>
        /// <remarks>
        ///     <para>⚡ Uses <see cref="DefaultAzureCredential"/> to authenticate with Azure Blob Storage.</para>
        ///     <para>⚡ File will be deleted (if new) or reverted (if existing) when the <see cref="TemporaryBlobFile"/> is disposed.</para>
        /// </remarks>
        /// <param name="blobContainerUri">
        ///     <para>The <see cref="BlobContainerClient.Uri" /> referencing the blob container that includes the name of the account and the name of the container.</para>
        ///     <para>This is likely to be similar to <a href="">https://{account_name}.blob.core.windows.net/{container_name}</a>.</para>
        /// </param>
        /// <param name="blobName">The name of the blob to upload.</param>
        /// <param name="blobContent">The content of the blob to upload.</param>
        /// <param name="logger">The logger to write diagnostic messages during the upload process.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="blobContainerUri"/> or the <paramref name="blobName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobContainerUri"/> or the <paramref name="blobContent"/> is <c>null</c>.</exception>
        public static Task<TemporaryBlobFile> UpsertFileAsync(Uri blobContainerUri, string blobName, BinaryData blobContent, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(blobContainerUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

            var containerClient = new BlobContainerClient(blobContainerUri, new DefaultAzureCredential());
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            return UpsertFileAsync(blobClient, blobContent, logger);
        }

        private static async Task<(bool createdByUs, BinaryData originalData)> EnsureBlobContentCreatedAsync(
            BlobClient client,
            BinaryData newContent,
            ILogger logger)
        {
            if (await client.ExistsAsync().ConfigureAwait(false))
            {
                BlobDownloadResult originalContent = await client.DownloadContentAsync().ConfigureAwait(false);

                logger.LogSetupReplaceFile(client.Name, client.AccountName, client.BlobContainerName);
                await client.UploadAsync(newContent, overwrite: true).ConfigureAwait(false);

                return (createdByUs: false, originalContent.Content);
            }

            logger.LogSetupUploadNewFile(client.Name, client.AccountName, client.BlobContainerName);
            await client.UploadAsync(newContent).ConfigureAwait(false);

            return (createdByUs: true, originalData: null);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_createdByUs)
            {
                _logger.LogTeardownDeleteFile(Client.Name, Client.AccountName, Client.BlobContainerName);
                await Client.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else if (_originalData != null)
            {
                _logger.LogTeardownRevertFile(Client.Name, Client.AccountName, Client.BlobContainerName);
                await Client.UploadAsync(_originalData, overwrite: true).ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }
    }

    internal static partial class TempBlobFileILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Upload Azure Blob file '{BlobName}' to container '{AccountName}/{ContainerName}'")]
        internal static partial void LogSetupUploadNewFile(this ILogger logger, string blobName, string accountName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Replace already existing Azure Blob file '{BlobName}' in container '{AccountName}/{ContainerName}'")]
        internal static partial void LogSetupReplaceFile(this ILogger logger, string blobName, string accountName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Delete Azure Blob file '{BlobName}' from container '{AccountName}/{ContainerName}'")]
        internal static partial void LogTeardownDeleteFile(this ILogger logger, string blobName, string accountName, string containerName);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert replaced Azure Blob file '{BlobName}' to original content in container '{AccountName}/{ContainerName}'")]
        internal static partial void LogTeardownRevertFile(this ILogger logger, string blobName, string accountName, string containerName);
    }
}
