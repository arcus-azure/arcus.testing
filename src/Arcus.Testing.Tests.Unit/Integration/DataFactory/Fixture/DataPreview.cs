using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bogus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Xunit.Assert;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture
{
    public class DataPreview
    {
        private readonly string[] _headers;
        private readonly JsonArray _data;

        private DataPreview(string[] headers, JsonArray data)
        {
            _headers = headers;
            _data = data;
        }

        public static DataPreview Create(JsonObject obj)
        {
            string[] headers = SerializeHeaders(obj);
            JsonArray data = Assert.IsType<JsonArray>(JsonSerializer.SerializeToNode(new[] { SerializeData(obj) }));

            return new DataPreview(headers, data);
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
                else if (node.Value is JsonArray arrOfDirectValues && arrOfDirectValues.All(elem => elem is JsonValue))
                {
                    headers.Add(headerName + " as string[]");
                }
                else if (node.Value is JsonObject child)
                {
                    headers.Add(headerName + " as (" + string.Join(", ", SerializeHeaders(child)) + ")");
                }
                else if (node.Value is JsonArray arr && arr.All(elem => elem is JsonObject))
                {
                    string[] inner = SerializeHeaders(arr.First().AsObject()).ToArray();
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
                (string key, JsonNode node) = obj.ElementAt(index);
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
            string headersTxt = "output(" + string.Join(", ", _headers) + ")";
            var json = $"{{ \"schema\": \"{headersTxt}\", \"data\": {_data.ToJsonString(new JsonSerializerOptions(new JsonSerializerOptions { WriteIndented = false }))} }}";
            var node = JsonNode.Parse(json);

            var x = node.ToString();

            var obj = Assert.IsType<JsonObject>(JsonNode.Parse("{}"));
            obj["output"] = x;

            return obj.ToString();
        }
    }
}
