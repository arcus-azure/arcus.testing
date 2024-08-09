using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Arcus.Testing
{
    public static class TemporaryCosmosMongoDbCollection
    {
        public static async Task<TemporaryCosmosMongoDbCollection<TDocument>> CreateIfNotExistsAsync<TDocument>(
            IMongoDatabase database,
            string collectionName,
            ILogger logger)
        {
            IAsyncCursor<BsonDocument> collections = await database.ListCollectionsAsync(new ListCollectionsOptions
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName)
            });

            if (await collections.AnyAsync())
            {

            }
        }


    }

    /// <summary>
    /// 
    /// </summary>
    public class TemporaryCosmosMongoDbCollection<TDocument> : IAsyncDisposable
    {
        

        public async ValueTask DisposeAsync()
        {
            
        }
    }
}
