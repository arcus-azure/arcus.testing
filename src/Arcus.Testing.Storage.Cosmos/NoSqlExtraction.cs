using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Cosmos;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how the NoSql-related information is extracted from items.
    /// </summary>
    internal static class NoSqlExtraction
    {
        internal static readonly JsonNodeOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };
        internal static readonly JsonSerializerOptions SerializeToNodeOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Extracts the unique ID from the <paramref name="node"/>.
        /// </summary>
        internal static string ExtractIdFromItem(JsonNode node, Type itemType = null)
        {
            string typeDescription = itemType is null ? "type" : $"'{itemType.Name}' type";
            if (node is not JsonObject item || !item.TryGetPropertyValue("id", out JsonNode idNode) || idNode is not JsonValue id)
            {
                throw new NotSupportedException(
                    $"[Test:Setup] Cannot temporary insert/delete NoSql items in NoSql container as no required 'id' JSON property was found for the {typeDescription}, " +
                    $"please make sure that there exists such a property in the type (Microsoft uses Newtonsoft.Json behind the scenes)");
            }

            if (!id.TryGetValue(out string itemId) || string.IsNullOrWhiteSpace(itemId))
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
        internal static PartitionKey ExtractPartitionKeyFromItem(JsonNode item, ContainerProperties properties)
        {
            string[][] partitionKeyTokens =
                properties.PartitionKeyPaths.Select(path => path.Split('/', StringSplitOptions.RemoveEmptyEntries)).ToArray();

            List<(bool isNone, JsonNode node)> nodeTree = new(partitionKeyTokens.Length);
            foreach (string[] token in partitionKeyTokens)
            {
                bool foundNode = TryParseNodeByToken(item, token, out JsonNode result);
                nodeTree.Add((isNone: !foundNode, node: result));
            }

            PartitionKey partitionKey = CreatePartitionKeyForNodeTree(nodeTree);
            return partitionKey;
        }

        private static bool TryParseNodeByToken(JsonNode pathTraversal, IReadOnlyList<string> tokens, out JsonNode result)
        {
            result = null;
            for (var i = 0; i < tokens.Count - 1; i++)
            {
                if (pathTraversal is not JsonObject next || !next.TryGetPropertyValue(tokens[i], out pathTraversal))
                {
                    return false;
                }
            }

            return pathTraversal is JsonObject last && last.TryGetPropertyValue(tokens[^1], out result);
        }

        private static PartitionKey CreatePartitionKeyForNodeTree(IEnumerable<(bool isNone, JsonNode node)> cosmosElementList)
        {
            var builder = new PartitionKeyBuilder();
            foreach ((bool isNone, JsonNode node) in cosmosElementList)
            {
                if (isNone)
                {
                    builder.AddNoneType();
                }
                else
                {
                    if (node is null)
                    {
                        builder.AddNullValue();
                    }
                    else
                    {
                        JsonValueKind kind = node.GetValueKind();
                        _ = kind switch
                        {
                            JsonValueKind.String => builder.Add(node.GetValue<string>()),
                            JsonValueKind.Number => builder.Add(float.Parse(node.GetValue<string>())),
                            JsonValueKind.True or JsonValueKind.False => builder.Add(node.GetValue<bool>()),
                            JsonValueKind.Null => builder.AddNullValue(),
                            _ => throw new ArgumentOutOfRangeException(nameof(cosmosElementList), kind, "Unsupported partition key value"),
                        };
                    }
                }
            }

            return builder.Build();
        }
    }
}
