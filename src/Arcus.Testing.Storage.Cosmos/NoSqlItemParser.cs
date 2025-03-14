using System;
using System.IO;
using System.Text;
using Microsoft.Azure.Cosmos;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how NoSql-items are parsed.
    /// </summary>
    internal static class NoSqlItemParser
    {
        /// <summary>
        /// Parse a raw <paramref name="json"/> NoSql item to a typed <typeparamref name="TItem"/>.
        /// </summary>
        internal static TItem Parse<TItem>(CosmosClient client, NoSqlItem json, TestPhase phase)
        {
            string prefix = phase switch
            {
                TestPhase.Setup => "[Test:Setup]",
                TestPhase.Teardown => "[Test:Teardown]",
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown test phase")
            };

            if (client.ClientOptions.Serializer is null)
            {
                throw new InvalidOperationException(
                    $"{prefix} Cannot match the NoSql item because the Cosmos client used has no JSON item serializer configured");
            }

            using var body = new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()));

            var item = client.ClientOptions.Serializer.FromStream<TItem>(body);
            if (item is null)
            {
                throw new InvalidOperationException(
                    $"{prefix} Cannot match the NoSql item because the configured JSON item serializer returned 'null' when deserializing '{typeof(TItem).Name}'");
            }

            return item;
        }
    }
}