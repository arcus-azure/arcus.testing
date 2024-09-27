using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryTableTests
    {
        [Fact]
        public async Task CreateTempTable_WithoutTableEndpoint_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTable.CreateIfNotExistsAsync(tableEndpoint: null, "tableName", NullLogger.Instance));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public async Task CreateTempTable_WithoutTableName_Fails(string tableName)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => TemporaryTable.CreateIfNotExistsAsync(new Uri("https://table-endpoint"), tableName, NullLogger.Instance));
        }
    }
}
