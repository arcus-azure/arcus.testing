using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bogus;

namespace Arcus.Testing.Tests.Core.Assert_.Fixture
{
    /// <summary>
    /// Represents a test fixture that generates random JSON contents.
    /// </summary>
    public class TestJson
    {
        private JsonNode _doc;
        private static readonly Faker Bogus = new();

        private TestJson(JsonNode doc)
        {
            _doc = doc;
        }

        /// <summary>
        /// Generates random JSON contents with randomly number of objects, arrays, properties and values.
        /// </summary>
        /// <returns></returns>
        public static TestJson Generate()
        {
            string json = 
                Bogus.Random.Bool()
                    ? GenerateJsonObject()
                    : GenerateJsonArray();

            return new TestJson(JsonNode.Parse(json));
        }

        /// <summary>
        /// Generates random JSON array with randomly number of objects, arrays, properties and values.
        /// </summary>
        public static TestJson GenerateArray()
        {
            return new TestJson(GenerateJsonArray());
        }

        private static string GenerateJsonArray()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[");

            int length = Bogus.Random.Int(1, 10);
            if (Bogus.Random.Bool())
            {
                builder.AppendLine(string.Join(",", Bogus.Make(length, () => GenerateJsonObject(maxDepth: 0))));
            }
            else
            {
                var elements =
                    Bogus.PickRandom(
                        Bogus.Make(length, () => Bogus.Random.Int().ToString()),
                        Bogus.Make(length, () => "\"" + Bogus.Lorem.Word() + "\""),
                        Bogus.Make(length, () => GenerateJsonObject(maxDepth: 0)));

                builder.AppendLine(string.Join(",", elements));
            }

            builder.AppendLine("]");
            return builder.ToString();
        }

        /// <summary>
        /// Generates random JSON object with randomly number of objects, arrays, properties and values.
        /// </summary>
        public static TestJson GenerateObject()
        {
            return new TestJson(JsonNode.Parse(GenerateJsonObject()));
        }

        private static string GenerateJsonObject(int? maxDepth = 3)
        {
            StringBuilder Recurse(StringBuilder acc, int depth)
            {
                if (depth >= maxDepth)
                {
                    string nodeName = CreateNodeName();
                    string nodeValue = CreateNodeValue();
                    acc.AppendLine($"\"{nodeName}\": {nodeValue}");

                    return acc;
                }

                string[] nodeNames = Bogus.Make(Bogus.Random.Int(2, 10), CreateNodeName).ToArray();
                for (var index = 0; index < nodeNames.Length; index++)
                {
                    string name = nodeNames[index];
                    if (Bogus.Random.Bool())
                    {
                        string nodeValue = CreateNodeValue();
                        acc.AppendLine($"\"{name}\": {nodeValue}");
                    }
                    else if (Bogus.Random.Bool())
                    {
                        string nodeName = CreateNodeName();
                        acc.AppendLine($"\"{nodeName}\": {{");
                        Recurse(acc, depth + 1);
                        acc.AppendLine("}");
                    }
                    else
                    {
                        string nodeName = CreateNodeName();
                        acc.AppendLine($"\"{nodeName}\": {GenerateJsonArray()}");
                    }

                    bool isNotLast = index < nodeNames.Length - 1;
                    if (isNotLast)
                    {
                        acc.Append(',');
                    }
                }

                return acc;
            }

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder = Recurse(builder, 0);
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string CreateNodeValue()
        {
            return Bogus.PickRandom(
                Bogus.Random.Int().ToString(),
                "\"" + Bogus.Lorem.Word() + "\"",
                Bogus.Random.Bool().ToString().ToLower());
        }

        private static string CreateNodeName()
        {
            return Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..10];
        }

        /// <summary>
        /// Inserts a property at a random location within the contents.
        /// </summary>
        public void InsertProperty(string name)
        {
            SelectRandomly()[name] = JsonNode.Parse("{}");
        }

        private JsonObject SelectRandomly(Func<JsonObject, bool> filter = null)
        {
            return _doc is JsonObject obj ? SelectRandomly(obj, filter) : SelectRandomly((JsonArray) _doc, filter);
        }

        private static JsonObject SelectRandomly(JsonObject node, Func<JsonObject, bool> filter = null)
        {
            return Bogus.PickRandom(
                node.Where(p => p.Value is JsonObject obj && (filter?.Invoke(obj) ?? true))
                    .Select(p => SelectRandomly((JsonObject) p.Value, filter))
                    .Append(node));
        }

        private static JsonObject SelectRandomly(JsonArray node, Func<JsonObject, bool> filter = null)
        {
            return Bogus.PickRandom(
                node.Where(p => p is JsonObject obj && (filter?.Invoke(obj) ?? true))
                    .Select(p => SelectRandomly((JsonObject) p, filter)));
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public TestJson Copy()
        {
            return new TestJson(JsonNode.Parse(_doc.ToString()));
        }

        /// <summary>
        /// Moves the nodes randomly around the document.
        /// </summary>
        public void Shuffle()
        {
            _doc = Shuffle(_doc);
        }

        private static JsonNode Shuffle(JsonNode node)
        {
            if (node is JsonArray array)
            {
                IEnumerable<JsonNode> items =
                    array.OfType<JsonObject>().Any()
                        ? array.Select(Shuffle)
                        : Bogus.Random.Shuffle(array);

                var json = JsonNode.Parse(JsonSerializer.Serialize(items));
                return json;
            }

            if (node is JsonObject obj)
            {
                IDictionary<string, JsonNode> items = 
                    Bogus.Random.Shuffle(obj)
                         .ToDictionary(p => p.Key, p => Shuffle(p.Value));

                var json = JsonNode.Parse(JsonSerializer.Serialize(items));
                return json;
            }

            return node;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _doc.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
