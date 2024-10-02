using System;
using System.Threading.Tasks;
using Azure.Identity;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryTableTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public async Task CreateTempTable_WithoutTableEndpoint_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTable.CreateIfNotExistsAsync(tableEndpoint: null, new DefaultAzureCredential(), "tableName", NullLogger.Instance));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTable_WithoutTableName_Fails(string tableName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTable.CreateIfNotExistsAsync(new Uri("https://table-endpoint"), new DefaultAzureCredential(), tableName, NullLogger.Instance));
        }

        [Fact]
        public void SetupCleanMatching_WithoutFilter_Fails()
        {
            // Arrange
            var options = new TemporaryTableOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingEntities(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingEntities(TableEntityFilter.RowKeyEqual("<row-key>"), null));
        }

        [Fact]
        public void TeardownCleanMatching_WithoutFilter_Fails()
        {
            // Arrange
            var options = new TemporaryTableOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingEntities(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingEntities(TableEntityFilter.PartitionKeyEqual("<partition-key>"), null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void RowKeyEqual_WithoutRowKey_Fails(string rowKey)
        {
            Assert.ThrowsAny<ArgumentException>(() => TableEntityFilter.RowKeyEqual(rowKey));
            Assert.ThrowsAny<ArgumentException>(() => TableEntityFilter.RowKeyEqual(rowKey, Bogus.PickRandom<StringComparison>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void PartitionKeyEqual_WithoutPartitionKey_Fails(string partitionKey)
        {
            Assert.ThrowsAny<ArgumentException>(() => TableEntityFilter.PartitionKeyEqual(partitionKey));
            Assert.ThrowsAny<ArgumentException>(() => TableEntityFilter.PartitionKeyEqual(partitionKey, Bogus.PickRandom<StringComparison>()));
        }

        [Fact]
        public void EntityEqual_WithoutFilter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => TableEntityFilter.EntityEqual(null));
        }
    }
}
