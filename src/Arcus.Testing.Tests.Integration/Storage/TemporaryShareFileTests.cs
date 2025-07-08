using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Xunit;
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
            var temp = await TemporaryShareFile.UpsertFileAsync(client, fileStream, Logger);

            Assert.Equal(client.Name, temp.Client.Name);
            return temp;
        }

        private async Task<FileShareTestContext> GivenFileShareAsync()
        {
            return await FileShareTestContext.GivenAvailableAsync(Configuration, Logger);
        }
    }
}
