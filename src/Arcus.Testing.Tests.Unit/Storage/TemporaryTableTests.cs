using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryTableTests
    {
        [Fact]
        public async Task CreateTempTable_WithoutClient_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTable.CreateIfNotExistsAsync(serviceClient: null, "tableName", NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTable.CreateIfNotExistsAsync(serviceClient: null, "tableName", NullLogger.Instance, opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTable_WithoutTableName_Fails(string tableName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTable.CreateIfNotExistsAsync(Mock.Of<TableServiceClient>(), tableName, NullLogger.Instance));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => TemporaryTable.CreateIfNotExistsAsync(Mock.Of<TableServiceClient>(), tableName, NullLogger.Instance, opt => { }));
        }

        [Fact]
        public void SetupCleanMatching_WithoutFilter_Fails()
        {
            // Arrange
            var options = new TemporaryTableOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingEntities((Func<TableEntity, bool>) null));
        }

        [Fact]
        public void TeardownCleanMatching_WithoutFilter_Fails()
        {
            // Arrange
            var options = new TemporaryTableOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingEntities((Func<TableEntity, bool>) null));
        }
    }
}
