using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryBlobContainerTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateBlobContainerIfNotExists_WithoutAccountName_Fails(string accountName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync(accountName, "<container-name>", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync(accountName, "<container-name>", NullLogger.Instance, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateBlobContainerIfNotExists_WithoutContainerName_Fails(string containerName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync("<account-name>", containerName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync("<account-name>", containerName, NullLogger.Instance, configureOptions: opt => { }));
        }

        [Fact]
        public async Task CreateBlobContainerIfNotExists_WithoutContainerClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync(containerClient: null, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryBlobContainer.CreateIfNotExistsAsync(containerClient: null, NullLogger.Instance, configureOptions: opt => { }));
        }
    }
}