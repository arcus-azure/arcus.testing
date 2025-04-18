using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares.Specialized;
using Bogus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using DirClient = Azure.Storage.Files.Shares.ShareDirectoryClient;
using FileClient = Azure.Storage.Files.Shares.ShareFileClient;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryShareDirectoryTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryShareDirectoryTests"/> class.
        /// </summary>
        public TemporaryShareDirectoryTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task CreateTempDirWithCleanAllOnSetup_ForExistingDir_RemovesAllItemsFromDirectoryUponCreation()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();
            FileClient file = await share.WhenFileAvailableAsync(dir);
            DirClient subDir = await share.WhenDirectoryAvailableAsync(dir);
            FileClient subFile = await share.WhenFileAvailableAsync(subDir);

            // Act
            await WhenTempDirCreatedAsync(dir, options =>
            {
                options.OnSetup.CleanAllItems();
            });

            // Assert
            await share.ShouldHaveDirectoriesAsync(dir);

            await share.ShouldNotHaveFilesAsync(file, subFile);
            await share.ShouldNotHaveDirectoriesAsync(subDir);
        }

        [Fact]
        public async Task CreateTempDirWithCleanAllOnTeardown_ForExistingDir_RemovesAllItemsFromDirectoryUponDisposal()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();

            FileClient fileBefore = await share.WhenFileAvailableAsync(dir);
            DirClient subDirBefore = await share.WhenDirectoryAvailableAsync(dir);
            FileClient subFileBefore = await share.WhenFileAvailableAsync(subDirBefore);

            // Act
            TemporaryShareDirectory temp = await WhenTempDirCreatedAsync(dir, options =>
            {
                options.OnTeardown.CleanAllItems();
            });

            // Assert
            await share.ShouldHaveDirectoriesAsync(dir);
            await share.ShouldHaveFilesAsync(fileBefore, subFileBefore);

            FileClient fileAfter = await share.WhenFileAvailableAsync(temp.Client);
            DirClient subDirAfter = await share.WhenDirectoryAvailableAsync(temp.Client);
            FileClient subFileAfter = await share.WhenFileAvailableAsync(subDirAfter);

            await temp.DisposeAsync();

            await share.ShouldNotHaveFilesAsync(fileBefore, subFileBefore, fileAfter, subFileAfter);
            await share.ShouldNotHaveDirectoriesAsync(subDirBefore, subDirAfter);
            await share.ShouldHaveDirectoriesAsync(dir);
        }

        [Fact]
        public async Task CreateTempDirWithCleanMatched_ForExistingDir_RemovesAllMatchingItems()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();

            FileClient fileMatchedBefore = await share.WhenFileAvailableAsync(dir);
            DirClient subDirNotMatchedBefore = await share.WhenDirectoryAvailableAsync(dir);
            DirClient subDirMatchedBefore = await share.WhenDirectoryAvailableAsync(dir);
            FileClient fileImplicitMatchedBefore = await share.WhenFileAvailableAsync(subDirMatchedBefore);

            // Act
            TemporaryShareDirectory temp = await WhenTempDirCreatedAsync(dir, options =>
            {
                options.OnSetup.CleanMatchingItems(f => f.Name == fileMatchedBefore.Name)
                               .CleanMatchingItems(f => f.Name == subDirMatchedBefore.Name);
            });

            // Assert
            await share.ShouldHaveDirectoriesAsync(dir, subDirNotMatchedBefore);
            await share.ShouldNotHaveFilesAsync(fileMatchedBefore, fileImplicitMatchedBefore);
            await share.ShouldNotHaveDirectoriesAsync(subDirMatchedBefore);

            FileClient fileMatchedAfter = await share.WhenFileAvailableAsync(dir);
            DirClient subDirNotMatchedAfter = await share.WhenDirectoryAvailableAsync(dir);
            DirClient subDirMatchedAfter = await share.WhenDirectoryAvailableAsync(dir);
            FileClient fileImplicitMatchedAfter = await share.WhenFileAvailableAsync(subDirMatchedAfter);

            temp.OnTeardown.CleanMatchingItems(f => f.Name == fileMatchedAfter.Name)
                           .CleanMatchingItems(f => f.Name == subDirMatchedAfter.Name);

            await temp.DisposeAsync();

            await share.ShouldHaveDirectoriesAsync(dir, subDirNotMatchedAfter);
            await share.ShouldNotHaveFilesAsync(fileMatchedAfter, fileImplicitMatchedAfter);
            await share.ShouldNotHaveDirectoriesAsync(subDirMatchedAfter);
        }

        [Fact]
        public async Task CreateTempDir_ForExistingDir_LeaveDirectoryAfterLifetimeFixture()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();
            FileClient fileBefore = await share.WhenFileAvailableAsync(dir);
            FileClient fileSelf = share.WhenFileUnavailable(dir);

            // Act
            TemporaryShareDirectory temp = await WhenTempDirCreatedAsync(dir);

            // Assert
            await share.ShouldHaveDirectoriesAsync(dir);

            await temp.WhenFileFileUploadAsync(fileSelf);
            FileClient fileAfter = await share.WhenFileAvailableAsync(temp.Client);
            DirClient subDirAfter = await share.WhenDirectoryAvailableAsync(temp.Client);
            FileClient subFileAfter = await share.WhenFileAvailableAsync(subDirAfter);

            await temp.DisposeAsync();

            await share.ShouldNotHaveFilesAsync(fileSelf);
            await share.ShouldHaveFilesAsync(fileAfter, subFileAfter, fileBefore);
            await share.ShouldHaveDirectoriesAsync(dir, subDirAfter);
        }

        [Fact]
        public async Task CreateTempDir_OnExistingShare_TemporaryCreateDirectoryOnShareDuringLifetimeFixture()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = share.WhenDirectoryUnavailable();
            FileClient file = share.WhenFileUnavailable(dir);

            // Act
            TemporaryShareDirectory temp = await WhenTempDirCreatedAsync(dir);

            // Assert
            await share.ShouldHaveDirectoriesAsync(dir);

            await share.WhenFileAvailableAsync(temp.Client);
            await temp.WhenFileFileUploadAsync(file);
            await share.WhenFileAvailableAsync(await share.WhenDirectoryAvailableAsync(temp.Client));

            await temp.DisposeAsync();

            await share.ShouldNotHaveFilesAsync(file);
            await share.ShouldNotHaveDirectoriesAsync(dir);
        }

        [Fact]
        public async Task CreateTempDir_OnNonExistingShare_FailsWithNotFound()
        {
            // Arrange
            await using var fileShare = await GivenFileShareAsync();

            ShareClient share = fileShare.WhenShareUnavailable();
            DirClient dir = fileShare.WhenDirectoryUnavailable(share);

            // Act / Assert
            await Assert.ThrowsAsync<DriveNotFoundException>(
                () => WhenTempDirCreatedAsync(dir));
        }

        private async Task<TemporaryShareDirectory> WhenTempDirCreatedAsync(DirClient dir, Action<TemporaryShareDirectoryOptions> configureOptions = null)
        {
            TemporaryShareDirectory temp;
            if (Bogus.Random.Bool())
            {
                ShareClient shareClient = dir.GetParentShareClient();
                temp = configureOptions is null
                    ? await TemporaryShareDirectory.CreateIfNotExistsAsync(shareClient, dir.Name, Logger)
                    : await TemporaryShareDirectory.CreateIfNotExistsAsync(shareClient, dir.Name, Logger, configureOptions);
            }
            else
            {
                temp = configureOptions is null
                    ? await TemporaryShareDirectory.CreateIfNotExistsAsync(dir, Logger)
                    : await TemporaryShareDirectory.CreateIfNotExistsAsync(dir, Logger, configureOptions);
            }

            Assert.Equal(dir.Name, temp.Client.Name);
            Assert.Equal(dir.AccountName, temp.Client.AccountName);

            return temp;
        }

        private async Task<FileShareTestContext> GivenFileShareAsync()
        {
            return await FileShareTestContext.GivenAvailableAsync(Configuration, Logger);
        }
    }

    internal static class TempShareDirExtensions
    {
        private static readonly Faker Bogus = new();

        internal static async Task WhenFileFileUploadAsync(this TemporaryShareDirectory dir, FileClient file)
        {
            await using var fileContents = new MemoryStream(Encoding.UTF8.GetBytes(Bogus.Lorem.Sentence()));
            await dir.UploadFileAsync(file.Name, fileContents);
        }
    }

    internal class FileShareTestContext : IAsyncDisposable
    {
        private readonly ShareClient _share;
        private readonly ShareServiceClient _service;
        private readonly ILogger _logger;

        private static readonly Faker Bogus = new();

        private FileShareTestContext(
            ShareServiceClient service,
            ShareClient share,
            ILogger logger)
        {
            _service = service;
            _share = share;
            _logger = logger ?? NullLogger.Instance;
        }

        public static async Task<FileShareTestContext> GivenAvailableAsync(TestConfig configuration, ILogger logger)
        {
            StorageAccount account = configuration.GetStorageAccount();

            var service = new ShareServiceClient(account.ConnectionString);

            string shareName = $"share-{Guid.NewGuid().ToString()[..10]}";
            ShareClient share = service.GetShareClient(shareName);

            logger.LogTrace("[Test:Setup] Create new Azure File share '{ShareName}' in account '{AccountName}'", share.Name, share.AccountName);
            await share.CreateIfNotExistsAsync();

            return new FileShareTestContext(service, share, logger);
        }

        public ShareClient WhenShareUnavailable()
        {
            string shareName = $"share-{Guid.NewGuid().ToString()[..10]}";
            ShareClient share = _service.GetShareClient(shareName);
            return share;
        }

        public async Task<DirClient> WhenDirectoryAvailableAsync()
        {
            DirClient sub = WhenDirectoryUnavailable();

            _logger.LogTrace("[Test] Create new Azure File share directory '{DirectoryName}' at '{DirectoryPath}'", sub.Name, sub.Path);
            await sub.CreateIfNotExistsAsync();

            return sub;
        }

        public async Task<DirClient> WhenDirectoryAvailableAsync(DirClient dirClient)
        {
            DirClient sub = WhenDirectoryUnavailable(dirClient);

            _logger.LogTrace("[Test] Create new Azure File share directory '{DirectoryName}' at '{DirectoryPath}'", sub.Name, sub.Path);
            await sub.CreateIfNotExistsAsync();

            return sub;
        }

        public DirClient WhenDirectoryUnavailable()
        {
            return WhenDirectoryUnavailable(_share);
        }

        public FileClient WhenFileUnavailable(DirClient dirClient)
        {
            return dirClient.GetFileClient($"file-{Guid.NewGuid().ToString()[..10]}");
        }

        public DirClient WhenDirectoryUnavailable(ShareClient dirClient)
        {
            string directoryName = $"dir-{Guid.NewGuid().ToString()[..10]}";
            DirClient sub = dirClient.GetDirectoryClient(directoryName);

            return sub;
        }

        public DirClient WhenDirectoryUnavailable(DirClient dirClient)
        {
            string directoryName = $"dir-{Guid.NewGuid().ToString()[..10]}";
            DirClient sub = dirClient.GetSubdirectoryClient(directoryName);

            return sub;
        }

        public async Task<FileClient> WhenFileAvailableAsync(DirClient directoryClient)
        {
            string fileName = $"file-{Guid.NewGuid().ToString()[..10]}";
            string fileContents = Bogus.Lorem.Sentence();
            _logger.LogTrace("[Test] Upload new Azure File share item '{FileName}' in directory '{AccountName}/{DirectoryPath}'", fileName, directoryClient.AccountName, directoryClient.Path);

            await using var contents = BinaryData.FromString(fileContents).ToStream();
            FileClient fileClient = await directoryClient.CreateFileAsync(fileName, contents.Length);
            await fileClient.UploadAsync(contents);

            return fileClient;
        }

        public async Task ShouldHaveDirectoriesAsync(params DirClient[] directories)
        {
            foreach (DirClient dir in directories)
            {
                Assert.True(await dir.ExistsAsync(), $"Azure File share directory '{dir.Name}' should be available on share '{dir.ShareName}', but it wasn't");
            }
        }

        public async Task ShouldHaveFilesAsync(params FileClient[] files)
        {
            foreach (var file in files)
            {
                Assert.True(await file.ExistsAsync(), $"Azure File share file '{file.Name}' should be available on share '{file.ShareName}', but it wasn't");
            }
        }

        public async Task<BinaryData> ShouldHaveFileAsync(FileClient file)
        {
            await ShouldHaveFilesAsync(file);
            using ShareFileDownloadInfo fileInfo = await file.DownloadAsync();

            var data = await BinaryData.FromStreamAsync(fileInfo.Content);
            return data;
        }

        public async Task ShouldNotHaveFilesAsync(params FileClient[] files)
        {
            foreach (var file in files)
            {
                Assert.False(await file.ExistsAsync(), $"Azure File share file '{file.Name}' should not be available on share '{file.ShareName}', but it was");
            }
        }

        public async Task ShouldNotHaveDirectoriesAsync(params DirClient[] directories)
        {
            foreach (var dir in directories)
            {
                Assert.False(await dir.ExistsAsync(), $"Azure File share directory '{dir.Name}' should not be available on share '{dir.ShareName}', but it was");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            disposables.Add(AsyncDisposable.Create(async () =>
            {
                _logger.LogTrace("[Test:Teardown] Fallback remove Azure File share '{ShareName}' in account '{AccountName}'", _share.Name, _share.AccountName);
                await _share.DeleteIfExistsAsync();
            }));
        }
    }
}
