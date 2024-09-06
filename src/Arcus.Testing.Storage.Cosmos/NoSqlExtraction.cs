using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Arcus.Testing
{
    internal static class NoSqlExtraction
    {
        internal static string ExtractIdFromItem(JObject item, Type itemType = null)
        {
            if (!item.TryGetValue("id", out JToken idNode) || idNode is not JValue id)
            {
                string typeDescription = itemType is null ? "type" : $"'{itemType.Name }' type";
                throw new NotSupportedException(
                    $"Cannot temporary insert/delete NoSql items in NoSql container as no required 'id' JSON property was found for the {typeDescription}, " +
                    $"please make sure that there exists such a property in the type (Microsoft uses Newtonsoft.Json behind the scenes)");
            }

            return id.Value<string>();
        }

        internal static PartitionKey ExtractPartitionKeyFromItem(ContainerProperties properties, JObject item)
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
