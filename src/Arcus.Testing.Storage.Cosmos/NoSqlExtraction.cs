using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how the NoSql-related information is extracted from items.
    /// </summary>
    internal static class NoSqlExtraction
    {
        /// <summary>
        /// Extracts the unique ID from the <paramref name="item"/>.
        /// </summary>
        internal static string ExtractIdFromItem(JObject item, Type itemType = null)
        {
            string typeDescription = itemType is null ? "type" : $"'{itemType.Name}' type";
            if (!item.TryGetValue("id", out JToken idNode) || idNode is not JValue id)
            {
                throw new NotSupportedException(
                    $"[Test:Setup] Cannot temporary insert/delete NoSql items in NoSql container as no required 'id' JSON property was found for the {typeDescription}, " +
                    $"please make sure that there exists such a property in the type (Microsoft uses Newtonsoft.Json behind the scenes)");
            }

            var itemId = id.Value<string>();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] Cannot temporary insert NoSql item because the 'id' property of the serialized {typeDescription} is blank, " +
                    $"please provide an unique identifier to your item model");
            }

            return itemId;
        }

        /// <summary>
        /// Extracts the partition key from the <paramref name="item"/>.
        /// </summary>
        internal static PartitionKey ExtractPartitionKeyFromItem(JObject item, ContainerProperties properties)
        {
            string[][] partitionKeyTokens =
                properties.PartitionKeyPaths.Select(path => path.Split('/', StringSplitOptions.RemoveEmptyEntries)).ToArray();

            List<(bool isNone, JToken node)> nodeTree = new(partitionKeyTokens.Length);
            foreach (string[] token in partitionKeyTokens)
            {
                bool foundNode = TryParseNodeByToken(item, token, out JToken result);
                nodeTree.Add((isNone: !foundNode, node: result));
            }

            PartitionKey partitionKey = CreatePartitionKeyForNodeTree(nodeTree);
            return partitionKey;
        }

        private static bool TryParseNodeByToken(JToken pathTraversal, IReadOnlyList<string> tokens, out JToken result)
        {
            result = null;
            for (var i = 0; i < tokens.Count - 1; i++)
            {
                if (pathTraversal is not JObject next || !next.TryGetValue(tokens[i], out pathTraversal))
                {
                    return false;
                }
            }

            return pathTraversal is JObject last && last.TryGetValue(tokens[^1], out result);
        }

        private static PartitionKey CreatePartitionKeyForNodeTree(IEnumerable<(bool isNone, JToken node)> cosmosElementList)
        {
            var builder = new PartitionKeyBuilder();
            foreach ((bool isNone, JToken node) in cosmosElementList)
            {
                if (isNone)
                {
                    builder.AddNoneType();
                }
                else
                {
                    _ = node?.Type switch
                    {
                        JTokenType.String or JTokenType.Guid or JTokenType.Uri => builder.Add(node.Value<string>()),
                        JTokenType.Float or JTokenType.Integer => builder.Add(float.Parse(node.Value<string>())),
                        JTokenType.Boolean => builder.Add(node.Value<bool>()),
                        null or JTokenType.Null => builder.AddNullValue(),
                        _ => throw new ArgumentOutOfRangeException(nameof(cosmosElementList), node.Type, "Unsupported partition key value"),
                    };
                }
            }

            return builder.Build();
        }
    }
}