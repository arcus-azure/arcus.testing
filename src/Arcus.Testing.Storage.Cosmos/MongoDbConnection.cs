using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how the test infrastructure connects with the Azure Cosmos MongoDb resource.
    /// </summary>
    internal static class MongoDbConnection
    {
        private static readonly HttpClient HttpClient = new();

        /// <summary>
        /// Authenticates a <see cref="MongoClient"/> with <see cref="DefaultAzureCredential"/>.
        /// </summary>
        internal static async Task<MongoClient> AuthenticateMongoClientAsync(ResourceIdentifier cosmosDbResourceId, string databaseName, string collectionName, ILogger logger)
        {
            AccessToken accessToken = await RequestAccessTokenAsync(logger);
            string responseBody = await RequestConnectionStringsAsync(cosmosDbResourceId, databaseName, collectionName, accessToken, logger);

            string connectionString = ParseConnectionString(responseBody, logger);
            return new MongoClient(connectionString);
        }

        private static async Task<AccessToken> RequestAccessTokenAsync(ILogger logger)
        {
            const string scope = "https://management.azure.com/.default";
            var tokenProvider = new DefaultAzureCredential();

            logger.LogTrace("Requesting access for test host at '{Url}'", scope);
            return await tokenProvider.GetTokenAsync(new TokenRequestContext(scopes: new[] { scope }));
        }

        private static async Task<string> RequestConnectionStringsAsync(
            ResourceIdentifier cosmosDbResourceId, 
            string databaseName, 
            string collectionName, 
            AccessToken accessToken, 
            ILogger logger)
        {
            var listConnectionStringUrl = $"https://management.azure.com/{cosmosDbResourceId}/listConnectionStrings?api-version=2021-04-15";
            logger.LogTrace("Requesting access for Azure Cosmos MongoDb resource at '{Url}'", listConnectionStringUrl);

            using var request = new HttpRequestMessage(HttpMethod.Post, listConnectionStringUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
            
            using HttpResponseMessage response = await HttpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new RequestFailedException(
                    (int) response.StatusCode,
                    $"Cannot contact Azure Cosmos MongoDb collection named '{collectionName}' at database '{databaseName}' in account '{cosmosDbResourceId.Name}', " +
                    $"because the test host could not successfully request access from the resource at '{listConnectionStringUrl}': {responseBody}");
            }

            return responseBody;
        }

        private static string ParseConnectionString(string responseBody, ILogger logger)
        {
            try
            {
                var root = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(responseBody);
                if (root != null
                    && root.TryGetValue("connectionStrings", out List<Dictionary<string, string>> connectionStrings)
                    && connectionStrings is { Count: > 0 })
                {
                    Dictionary<string, string> primaryConnectionStringSet = connectionStrings[0];
                    if (primaryConnectionStringSet != null &&
                        primaryConnectionStringSet.TryGetValue("connectionString", out string primaryConnectionString))
                    {
                        return primaryConnectionString;
                    }
                }
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Failed to parse the response for Azure Cosmos MongoDb access due to a deserialization failure: {Message}", exception.Message);
                throw;
            }

            throw new JsonException(
                $"Failed to parse the response for Azure Cosmos MongoDb access as there does not exists any access information in the response: {responseBody}");
        }
    }
}
