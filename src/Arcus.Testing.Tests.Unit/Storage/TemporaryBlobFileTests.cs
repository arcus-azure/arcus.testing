using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryBlobFileTests
    {
        [Fact]
        public async Task UploadBlobFileIfNotExists_WithoutContainerUri_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    blobContainerUri: null,
                    "<blob-name>",
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance));

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    blobContainerUri: null,
                    "<blob-name>",
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance,
                    configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task UploadBlobFileIfNotExistsViaContainerUri_WithoutBlobName_Fails(string blobName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    new Uri("https://some-url"),
                    blobName,
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance));

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    new Uri("https://some-url"),
                    blobName,
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance,
                    configureOptions: opt => { }));
        }

        [Fact]
        public async Task UploadBlobFileIfNotExistsViaContainerUri_WithoutBlobContent_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    new Uri("https://some-url"),
                    "<blob-name>",
                    blobContent: null,
                    NullLogger.Instance));

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    new Uri("https://some-url"),
                    "<blob-name>",
                    blobContent: null,
                    NullLogger.Instance,
                    configureOptions: opt => { }));
        }

        [Fact]
        public async Task UploadBlobFileIfNotExists_WithoutBlobClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    blobClient: null,
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance));

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    blobClient: null,
                    BinaryData.FromString("<blob-content>"),
                    NullLogger.Instance,
                    configureOptions: opt => { }));
        }

        [Fact]
        public async Task UploadBlobFileIfNotExistsViaBlobClient_WithoutBlobContent_Fails()
        {
            var client = Mock.Of<BlobClient>();
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    client,
                    blobContent: null,
                    NullLogger.Instance));

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryBlobFile.UploadIfNotExistsAsync(
                    client,
                    blobContent: null,
                    NullLogger.Instance,
                    opt => { }));
        }
    }
}