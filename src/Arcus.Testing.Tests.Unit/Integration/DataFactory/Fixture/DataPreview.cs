﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture
{
    /// <summary>
    /// Represents the data preview serialized output of a run DataFlow.
    /// </summary>
    public class DataPreview
    {
        private readonly string _headersTxt;
        private readonly JsonArray _data;

        private DataPreview(string headersTxt, JsonArray data)
        {
            _headersTxt = headersTxt;
            _data = data;
        }

        /// <summary>
        /// Creates a data preview based on the raw headers and data array.
        /// </summary>
        public static DataPreview Create(string headersTxt, string dataTxt)
        {
            headersTxt = headersTxt.StartsWith("output(") ? headersTxt : $"output({headersTxt})";
            var data = Assert.IsType<JsonArray>(JsonNode.Parse(dataTxt));

            return new DataPreview(headersTxt, data);
        }

        /// <summary>
        /// Creates a data preview based on the rows of a CSV table.
        /// </summary>
        public static DataPreview Create(CsvTable csv)
        {
            string headersTxt = $"output({string.Join(", ", csv.HeaderNames.Select(h => $"{CreateHeaderName(h)} as string"))})";
            var data = JsonSerializer.SerializeToNode(csv.Rows.Select(r => JsonSerializer.SerializeToNode(r.Cells.Select(c => c.Value))));
            var arr = Assert.IsType<JsonArray>(data);

            return new DataPreview(headersTxt, arr);
        }

        /// <summary>
        /// Creates a data preview based on the property names of a JSON object.
        /// </summary>
        public static DataPreview Create(JsonObject obj)
        {
            string[] headers = SerializeHeaders(obj);
            JsonArray data = Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(new[] { SerializeData(obj) }));

            return new DataPreview("output(" + string.Join(", ", headers) + ")", data);
        }

        /// <summary>
        /// Creates a data preview based on the elements of a JSON array.
        /// </summary>
        public static DataPreview Create(JsonArray arr)
        {
            var obj = arr[0].AsObject();
            string[] inner = SerializeHeaders(obj).ToArray();
            JsonArray data = Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(arr.Cast<JsonObject>().Select(SerializeData).ToArray()));

            return new DataPreview("output(" + string.Join(", ", inner) + ")", data);
        }

        private static string[] SerializeHeaders(JsonObject obj)
        {
            var headers = new Collection<string>();
            foreach (var node in obj)
            {
                string headerName = CreateHeaderName(node.Key);
                if (node.Value is JsonValue)
                {
                    headers.Add(headerName + " as string");
                }
                else if (node.Value is JsonArray arrOfDirectValues && (arrOfDirectValues.Count == 0 || arrOfDirectValues.All(elem => elem is JsonValue)))
                {
                    headers.Add(headerName + " as string[]");
                }
                else if (node.Value is JsonObject child)
                {
                    headers.Add(headerName + " as (" + string.Join(", ", SerializeHeaders(child)) + ")");
                }
                else if (node.Value is JsonArray arr && arr.All(elem => elem is JsonObject))
                {
                    string[] inner = SerializeHeaders(arr[0].AsObject()).ToArray();
                    headers.Add(headerName + " as (" + string.Join(", ", inner) + ")[]");
                }
            }

            return headers.ToArray();
        }

        private static string CreateHeaderName(string name)
        {
            return name.Contains("-") ? $"{{{name}}}" : name;
        }

        private static JsonArray SerializeData(JsonObject obj)
        {
            var arr = new Collection<object>();

            for (var index = 0; index < obj.Count; index++)
            {
                (_, JsonNode node) = obj.ElementAt(index);
                if (node is JsonValue value)
                {
                    arr.Add(value.ToString());
                }
                else if (node is JsonObject child)
                {
                    arr.Add(SerializeData(child));
                }
                else if (node is JsonArray arrOfValues && arrOfValues.All(elem => elem is JsonValue))
                {
                    arr.Add(arrOfValues);
                }
                else if (node is JsonArray arrayOfObjects && arrayOfObjects.All(elem => elem is JsonObject))
                {
                    var inner = JsonSerializer.SerializeToNode(arrayOfObjects.Cast<JsonObject>().Select(SerializeData));
                    arr.Add(inner);
                }
            }

            return Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(arr));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var json = $"{{ \"schema\": \"{_headersTxt}\", \"data\": {_data.ToJsonString(new JsonSerializerOptions(new JsonSerializerOptions { WriteIndented = false }))} }}";
            var node = JsonNode.Parse(json);

            var x = node.ToString();

            var obj = Assert.IsType<JsonObject>(JsonNode.Parse("{}"));
            obj["output"] = x;

            return obj.ToString();
        }
    }
}
