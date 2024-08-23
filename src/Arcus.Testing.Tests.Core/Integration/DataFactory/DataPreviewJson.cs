using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Core.Integration.DataFactory
{
    public static class DataPreviewJson
    {
        private static readonly Faker Bogus = new();

        public static JsonObject GenerateJsonObject()
        {
            string[] nodeNames = GenerateJsonNodeNames();
            JsonObject json = GenerateJsonObjectOfValues(nodeNames);

            return json;
        }

        private static JsonObject GenerateJsonObjectOfValues(string[] nodeNames)
        {
            Dictionary<string, JsonNode> directValues = nodeNames.Select(nodeName =>
            {
                JsonNode nodeValue = Bogus.PickRandom(1, 1, 1, 2, 2, 3, 4) switch
                {
                    1 => GenerateJsonValue(),
                    2 => GenerateJsonArrayOfValues(),
                    3 => GenerateJsonObjectOfValues(GenerateJsonNodeNames()),
                    4 => GenerateJsonArrayOfObjects()
                };

                return new KeyValuePair<string, JsonNode>(nodeName, nodeValue);
            }).ToDictionary(item => item.Key, item => item.Value);

            return Assert.IsType<JsonObject>(JsonSerializer.SerializeToNode(directValues));
        }

        private static JsonArray GenerateJsonArrayOfValues()
        {
            int type = Bogus.PickRandom(1, 2, 3);
            IList<JsonValue> values = Bogus.Make(Bogus.Random.Int(1, 2), () => type switch
            {
                1 => (JsonValue) Bogus.Lorem.Word(),
                2 => (JsonValue) Bogus.Random.Int(1, 100),
                3 => (JsonValue) Bogus.Random.Bool()
            });

            return Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(values));
        } 

        public static JsonArray GenerateJsonArrayOfObjects(int min = 1, int max = 2)
        {
            string[] nodeNames = GenerateJsonNodeNames();

            JsonObject obj = GenerateJsonObjectOfValues(nodeNames);
            JsonObject[] objects = Bogus.Make(Bogus.Random.Int(min, max), () => obj).ToArray();

            return Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(objects));
        }

        private static string[] GenerateJsonNodeNames()
        {
            return Bogus.Make(Bogus.Random.Int(1, 2), () => Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..3])
                        .ToArray();
        }

        private static JsonValue GenerateJsonValue()
        {
            return GenerateJsonValue(Bogus.Random.Int(1, 3));
        }

        private static JsonValue GenerateJsonValue(int type)
        {
            return type switch
            {
                1 => (JsonValue) Bogus.Lorem.Word(),
                2 => (JsonValue) Bogus.Random.Int(1, 100),
                3 => (JsonValue) Bogus.Random.Bool()
            };
        }
    }
}
