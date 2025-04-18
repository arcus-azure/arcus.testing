using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Storage.Configuration;
using Azure.Identity;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Xunit;
using Xunit.Abstractions;
using DirClient = Azure.Storage.Files.Shares.ShareDirectoryClient;
using FileClient = Azure.Storage.Files.Shares.ShareFileClient;

namespace Arcus.Testing.Tests.Integration.Storage
{
    public class TemporaryShareFileTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryShareFileTests"/> class.
        /// </summary>
        public TemporaryShareFileTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task UploadNewTempFile_OnExistingDir_TemporaryStoresFileOnDirDuringLifetimeFixture()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();
            FileClient file = share.WhenFileUnavailable(dir);
            await using Stream expected = CreateFileContents();

            // Act
            TemporaryShareFile temp = await WhenFileCreatedAsync(file, expected);

            // Assert
            BinaryData actual = await share.ShouldHaveFileAsync(file);
            AssertEqualContents(expected, actual);

            await temp.DisposeAsync();
            await share.ShouldNotHaveFilesAsync(file);
        }

        [Fact]
        public async Task UploadTempFile_OnExistingDirWithAlreadyStoredFile_TemporaryReplacesFileOnDirDuringLifetimeFixture()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = await share.WhenDirectoryAvailableAsync();
            FileClient file = await share.WhenFileAvailableAsync(dir);
            BinaryData originalContents = await share.ShouldHaveFileAsync(file);

            await using Stream newContents = CreateFileContents();

            // Act
            TemporaryShareFile temp = await WhenFileCreatedAsync(file, newContents);

            // Assert
            BinaryData actualBefore = await share.ShouldHaveFileAsync(file);
            AssertEqualContents(newContents, actualBefore);

            await temp.DisposeAsync();

            BinaryData actualAfter = await share.ShouldHaveFileAsync(file);
            AssertEqualContents(originalContents, actualAfter);
        }

        private static void AssertEqualContents(Stream expectedStream, BinaryData actual)
        {
            expectedStream.Position = 0;
            BinaryData expected = BinaryData.FromStream(expectedStream);

            AssertEqualContents(actual, expected);
        }

        private static void AssertEqualContents(BinaryData actual, BinaryData expected)
        {
            Assert.False(actual.ToMemory().IsEmpty, "actual share file contents are empty");
            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public async Task UploadTempFile_OnNonExistingDir_FailsWithNotFound()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            DirClient dir = share.WhenDirectoryUnavailable();
            FileClient file = share.WhenFileUnavailable(dir);

            await using Stream fileContents = CreateFileContents();

            // Act / Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => WhenFileCreatedAsync(file, fileContents));
        }

        [Fact]
        public async Task UploadTempFile_OnNonExistingShare_FailsWithNotFound()
        {
            // Arrange
            await using var share = await GivenFileShareAsync();

            ShareClient client = share.WhenShareUnavailable();
            DirClient dir = share.WhenDirectoryUnavailable(client);
            FileClient file = share.WhenFileUnavailable(dir);

            await using Stream fileContents = CreateFileContents();

            // Act / Assert
            await Assert.ThrowsAsync<DriveNotFoundException>(
                () => WhenFileCreatedAsync(file, fileContents));
        }

        private static Stream CreateFileContents()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(Bogus.Lorem.Sentence()));
        }

        private async Task<TemporaryShareFile> WhenFileCreatedAsync(FileClient client, Stream fileStream)
        {
            var temp = await TemporaryShareFile.CreateIfNotExistsAsync(client, fileStream, Logger);

            Assert.Equal(client.Name, temp.Client.Name);
            return temp;
        }

        private async Task<FileShareTestContext> GivenFileShareAsync()
        {
            return await FileShareTestContext.GivenAvailableAsync(Configuration, Logger);
        }

        [Fact]
        public async Task UploadFileShare_ToExistingDirectory_TemporaryOverridesFileContentsDuringLifetimeFixture()
        {
            // Arrange
            StorageAccount account = Configuration.GetStorageAccount();

            var service = new ShareServiceClient(
                new Uri($"https://{account.Name}.file.core.windows.net/"),
                new ClientSecretCredential(ServicePrincipal.TenantId, ServicePrincipal.ClientId, ServicePrincipal.ClientSecret));

            service = new ShareServiceClient(account.ConnectionString);

            string shareName = Guid.NewGuid().ToString();
            var share = service.GetShareClient(shareName);
            await share.CreateAsync();

            string directoryName = Guid.NewGuid().ToString();
            ShareDirectoryClient directoryClient = share.GetDirectoryClient(directoryName);
            await directoryClient.CreateAsync();

            string fileName = Guid.NewGuid().ToString();
            FileClient fileClient = directoryClient.GetFileClient(fileName);
            using Stream originalContents = new MemoryStream(Encoding.UTF8.GetBytes("123"));
            await fileClient.CreateAsync(originalContents.Length);

            try
            {
                await fileClient.UploadAsync(originalContents);

                using Stream fileContents = new MemoryStream(Encoding.UTF8.GetBytes("456"));
                var temp = await TemporaryShareFile.CreateIfNotExistsAsync(directoryClient, fileName, fileContents, Logger);

                Assert.Equal("456", await DownloadFileShareContentsAsStringAsync(fileClient));
                await temp.DisposeAsync();
                Assert.Equal("123", await DownloadFileShareContentsAsStringAsync(fileClient));
            }
            finally
            {
                await share.DeleteIfExistsAsync();
            }
        }

        private static async Task<string> DownloadFileShareContentsAsStringAsync(FileClient fileClient)
        {
            ShareFileDownloadInfo actual = await fileClient.DownloadAsync();
            using var reader = new StreamReader(actual.Content);
            string actualTxt = await reader.ReadToEndAsync();
            return actualTxt;
        }
    }
}
