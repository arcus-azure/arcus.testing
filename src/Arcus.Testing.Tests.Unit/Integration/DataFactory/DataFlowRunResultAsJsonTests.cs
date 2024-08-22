using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture;
using Bogus;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public class DataFlowRunResultAsJsonTests
    {
        private readonly ILogger _logger;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowRunResultAsJsonTests" /> class.
        /// </summary>
        public DataFlowRunResultAsJsonTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Property]
        public void GetDataAsJson_WithJsonObject_SucceedsByParsing()
        {
            // Arrange
            JsonObject expected = GenerateJsonObject();

            //JsonObject expected = (JsonObject)JsonNode.Parse(@"{
            //    ""category"": { ""name"": ""these products"" },
            //    ""description"": ""this is a description"",
            //    ""productId"": 123,
            //    ""related"": [ { ""productId"": 456, ""tags"": [ ""tag3"", ""tag4"" ] } ],
            //    ""tags"": [ ""tag1"", ""tag2"" ]
            //}");
            //JsonObject expected = (JsonObject)JsonNode.Parse(@"{
            //    ""a"": [ 
            //        { ""b"": { ""c"": 1 } },
            //        { ""b"": { ""c"": 4 } }
            //    ]
            //}");

            // Act
            var output = DataPreview.Create(expected).ToString();
            _logger.LogTrace("output: {Output}", output);

            DataFlowRunResult result = CreateRunResult(output);

            // Assert
            AssertJson.Equal(expected, result.GetDataAsJson());
        }

        private static JsonObject GenerateJsonObject()
        {
            string[] nodeNames = GenerateJsonNodeNames();
            return GenerateJsonObjectOfValues(nodeNames);
        }

        private static JsonObject GenerateJsonObjectOfValues(string[] nodeNames)
        {
            Dictionary<string, JsonNode> directValues = nodeNames.Select(nodeName =>
            {
                JsonNode nodeValue = Bogus.PickRandom(1, 1, 1, 2, 2, 3) switch
                {
                    1 => GenerateJsonValue(),
                    2 => GenerateJsonArrayOfValues(),
                    3 => GenerateJsonArrayOfObjects()
                };

                return new KeyValuePair<string, JsonNode>(nodeName, nodeValue);
            }).ToDictionary(item => item.Key, item => item.Value);

            return Assert.IsType<JsonObject>(JsonSerializer.SerializeToNode(directValues));
        }

        private static JsonArray GenerateJsonArrayOfValues()
        {
            int type = Bogus.PickRandom(1, 2, 3);
            IList<JsonValue> values = Bogus.Make(Bogus.Random.Int(1, 10), () => type switch
            {
                1 => (JsonValue) Bogus.Lorem.Word(),
                2 => (JsonValue) Bogus.Random.Int(1, 100),
                3 => (JsonValue) Bogus.Random.Bool()
            });

            return Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(values));
        } 

        private static JsonArray GenerateJsonArrayOfObjects()
        {
            string[] nodeNames = GenerateJsonNodeNames();

            JsonObject[] objects = Bogus.Make(Bogus.Random.Int(1, 1), () => GenerateJsonObjectOfValues(nodeNames)).ToArray();
            return Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(objects));
        }

        private static string[] GenerateJsonNodeNames()
        {
            return Bogus.Make(Bogus.Random.Int(1, 1), () => Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..3])
                        .ToArray();
        }

        private static JsonValue GenerateJsonValue()
        {
            return Bogus.PickRandom(
                (JsonValue) Bogus.Lorem.Word(),
                (JsonValue) Bogus.Random.Int(1, 100),
                (JsonValue) Bogus.Random.Bool());
        }

        private static JsonArray GenerateJsonArray()
        {
            IList<JsonNode> nodes = Bogus.Make(Bogus.Random.Int(2, 5), () => JsonNode.Parse(TestJson.GenerateObject().ToString()));
            JsonNode node = JsonSerializer.SerializeToNode(nodes);

            return Assert.IsType<JsonArray>(node);
        }

        [Theory]
        [InlineData(
            //$"{{ \"output\": \"{{  \\\"schema\\\": \\\"output(category as (name as string), description as string, productId as string, related as (productId as string, tags as string[])[], tags as string[])\\\",\\\\r\\\\n   \\\"data\\\": [[[\\\"these products\\\"], \\\"this is a description\\\",     123,     [  456, [ \"tag3\\\",\\\\r\\\\n          \\\"tag4\\\"\\\\r\\\\n        ]\\\\r\\\\n      ],\\\\r\\\\n      [\\\\r\\\\n        \\\"tag1\\\",\\\\r\\\\n        \\\"tag2\\\"\\\\r\\\\n      ]\\\\r\\\\n    ]\\\\r\\\\n  ]\\\\r\\\\n}}\\\"\\r\\n}}"
            $"{{ \"output\": \"{{   \\\"schema\\\": \\\"output(category as (name as string), description as string, productId as string, related as (productId as string, tags as string[])[], tags as string[])\\\" ,\\n        \\\"data\\\": [[[\\\"these products\\\"], \\\"this is a description\\\", \\\"123\\\", [[\\\"456\\\", [\\\"tag3\\\", \\\"tag4\\\"]]], [\\\"tag1\\\", \\\"tag2\\\"]]],\\n        \\\"metadata\\\": [\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\"]\\n      }}\\n      \" }}",
            "{ \"productId\": 123, \"description\": \"this is a description\", \"tags\": [ \"tag1\", \"tag2\" ],\r\n    \"related\": [ { \"productId\": 456, \"tags\": [ \"tag3\", \"tag4\" ] } ],\r\n    \"category\": { \"name\": \"these products\" }\r\n}")]
        public void GetDataAsJson_WithData_SucceedsByParsing(string runData, string expectedJson)
        {
            // Arrange
            DataFlowRunResult result = CreateRunResult(runData);

            // Act
            JsonNode json = result.GetDataAsJson();

            // Assert
            AssertJson.Equal(AssertJson.Load(expectedJson), json);
        }

        [Fact]
        public void Data()
        {
            //JsonObject outputObj = ParseOutputNode($"{{ \"output\": \"{{   \\\"schema\\\": \\\"output(related as (productId as string, tags as string[])[])\\\" ,\\n        \\\"data\\\": [[ [[\\\"456\\\", [\\\"tag3\\\", \\\"tag4\\\"]]] ]],\\n        \\\"metadata\\\": [\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\"]\\n      }}\\n      \" }}");
            JsonObject outputObj = ParseOutputNode($"{{ \"output\": \"{{   \\\"schema\\\": \\\"output(category as (name as string), description as string, productId as string, related as (productId as string, tags as string[])[], tags as string[])\\\" ,\\n        \\\"data\\\": [[[\\\"these products\\\"], \\\"this is a description\\\", \\\"123\\\", [[\\\"456\\\", [\\\"tag3\\\", \\\"tag4\\\"]]], [\\\"tag1\\\", \\\"tag2\\\"]]],\\n        \\\"metadata\\\": [\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\",\\\"{{\\\\\\\"java8Api\\\\\\\":\\\\\\\"false\\\\\\\"}}\\\"]\\n      }}\\n      \" }}");

            PreviewHeader[] headers = ParseHeaders(outputObj);

            JsonNode data = ParseData(headers, outputObj);
        }

        private static JsonNode ParseData(PreviewHeader[] headers, JsonObject outputObj)
        {
            if (!outputObj.TryGetPropertyValue("data", out JsonNode headersNode)
                || headersNode is not JsonArray dataArray)
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the headers are not available in the 'output.data' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            JsonNode[] results =
                dataArray.Where(elem => elem is JsonArray)
                         .Cast<JsonArray>()
                         .Select(arr => FillDataFrom(headers, arr))
                         .ToArray();

            return results.Length == 1 
                ? results[0] 
                : JsonSerializer.SerializeToNode(results);
        }

        private static JsonNode FillDataFrom(PreviewHeader[] headers, JsonArray dataArray)
        {
            if (headers.Length != dataArray.Count)
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the header count does not match the data count: {dataArray}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            var result = new Dictionary<string, JsonNode>();
            for (var i = 0; i < dataArray.Count; i++)
            {
                JsonNode headerValue = dataArray[i];
                PreviewHeader headerName = headers[i];

                switch (headerName.Type)
                {
                    case PreviewDataType.DirectValue:
                        result[headerName.Name] = float.TryParse(headerValue.ToString(), out float numeric) ? numeric : headerValue;
                        break;
                    
                    case PreviewDataType.Array when headerValue is JsonArray arr:
                        JsonNode[] elements = arr.Cast<JsonArray>().Select(elem => FillDataFrom(headerName.Children, elem)).ToArray();
                        result[headerName.Name] = JsonSerializer.SerializeToNode(elements);
                        break;
                    
                    case PreviewDataType.Object when headerValue is JsonArray inner:
                        JsonNode children = FillDataFrom(headerName.Children, inner);
                        result[headerName.Name] = JsonSerializer.SerializeToNode(children);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(headers), headerName, "Unknown preview data type");
                }
            }

            return JsonSerializer.SerializeToNode(result);
        }

        private static JsonObject ParseOutputNode(string previewAsJson)
        {
            JsonNode json = JsonNode.Parse(previewAsJson);
            if (json is not JsonObject)
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the content is loaded as 'null': {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string outputJson = json["output"]?.ToString();
            if (string.IsNullOrWhiteSpace(outputJson))
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the output is not available in the 'output' node: {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            JsonNode outputNode = JsonNode.Parse(outputJson);
            if (outputNode is not JsonObject outputObj)
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the 'output' node value is not considered valid JSON: {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            return outputObj;
        }

        private PreviewHeader[] ParseHeaders(JsonObject outputObj)
        {
            if (!outputObj.TryGetPropertyValue("schema", out JsonNode headersNode) 
                || headersNode is not JsonValue headersValue 
                || !headersValue.ToString().StartsWith("output"))
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the headers are not available in the 'output.schema' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string headersTxt = 
                Regex.Replace(headersValue.GetValue<string>(), "^output\\(", string.Empty).TrimEnd(')')
                     .Replace("\\n", "")
                     .Replace(", ", ",")
                     .Replace(" as string[]", " as string");

            (int _, PreviewHeader[] parsed) = ParseHeaders(startIndex: 0, headersTxt);
            return parsed;
        }

        [Fact]
        public void Test()
        {
            string headersTxt = "category as (name as string), description as string, productId as string, related as (productId as string,\\n tags as string[])[], tags as string[]";
            headersTxt = headersTxt.Replace("\\n", "")
                                   .Replace(", ", ",")
                                   .Replace(" as string[]", " as string");

            (int index, PreviewHeader[] parsed) = ParseHeaders(startIndex: 0, headersTxt);
        }

        private (int index, PreviewHeader[] parsed) ParseHeaders(int startIndex, string headersTxt)
        {
            var headers = new Collection<PreviewHeader>();

            var headerName = new StringBuilder();
            for (int i = startIndex; i < headersTxt.Length; i++)
            {
                char ch = headersTxt[i];

                if (ch == ')')
                {
                    if (headerName.Length > 0)
                    {
                        headers.Add(PreviewHeader.CreateAsValue(headerName.ToString()));
                    }

                    return (i, headers.ToArray());
                }

                if (ch == '(')
                {
                    (int currentIndex, PreviewHeader[] parsed) = ParseHeaders(i + 1, headersTxt);
                    if (currentIndex == headersTxt.Length - 1 || headersTxt[currentIndex + 1] == ',')
                    {
                        headers.Add(PreviewHeader.CreateAsObject(headerName.ToString(), parsed));
                        headerName.Clear();
                        i = currentIndex + 1;
                    }
                    else if (headersTxt[(currentIndex + 1)..(currentIndex + 3)] == "[]")
                    {
                        headers.Add(PreviewHeader.CreateAsArray(headerName.ToString(), parsed));
                        headerName.Clear();
                        i = currentIndex + 3;
                    }
                }

                if (char.IsLetterOrDigit(ch) || ch == ' ')
                {
                    headerName.Append(ch);
                }

                if (ch == ',' || i == headersTxt.Length - 1)
                {
                    var asString = " as string";
                    int min = i - asString.Length + 1;
                    int max = i + 1;
                    string part = headersTxt[min..max];
                    if (part == asString || part == "as string,")
                    {
                        headers.Add(PreviewHeader.CreateAsValue(headerName.ToString()));
                        headerName.Clear();
                    }
                }
            }

            return (-1, headers.ToArray());
        }

        public enum PreviewDataType { DirectValue, Array, Object }

        private class PreviewHeader
        {
            public string Name { get; set; }

            public PreviewDataType Type { get; set; }

            public PreviewHeader[] Children { get; set; }

            public static PreviewHeader CreateAsValue(string headerName)
            {
                return new PreviewHeader { Name = Regex.Replace(headerName, " as string$", ""), Type = PreviewDataType.DirectValue, Children = Array.Empty<PreviewHeader>() };
            }

            public static PreviewHeader CreateAsArray(string headerName, PreviewHeader[] parsed)
            {
                return new PreviewHeader { Name = Regex.Replace(headerName, "as $", ""), Type = PreviewDataType.Array, Children = parsed };
            }

            public static PreviewHeader CreateAsObject(string headerName, PreviewHeader[] parsed)
            {
                return new PreviewHeader { Name = Regex.Replace(headerName, " as $", ""), Type = PreviewDataType.Object, Children = parsed };
            }
        }

        private static DataFlowRunResult CreateRunResult(string input)
        {
            return new DataFlowRunResult(status: Bogus.Lorem.Word(), BinaryData.FromString(input));
        }
    }
}
