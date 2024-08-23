using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Arcus.Testing.Failure;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when loading the data preview of a DataFactory DataFlow run to a JSON node with <see cref="DataFlowRunResult.GetDataAsJson()"/>.
    /// </summary>
    public class DataPreviewJsonOptions
    {
        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        /// <summary>
        /// Gets or sets the specific culture of the loaded JSON nodes - this is especially useful when loading floating numbers.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is <c>null</c>.</exception>
        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set => _cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Represents the final result of the <see cref="TemporaryDataFlowDebugSession.RunDataFlowAsync(string,string)"/>.
    /// </summary>
    public class DataFlowRunResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowRunResult" /> class.
        /// </summary>
        public DataFlowRunResult(string status, BinaryData data)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status should not be blank", nameof(status));
            }

            Status = status;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Gets the run status of data preview, statistics or expression preview.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the raw data of data preview, statistics or expression preview.
        /// </summary>
        public BinaryData Data { get; }

        /// <summary>
        /// Tries to load the raw <see cref="Data"/> as a valid JSON node.
        /// </summary>
        public JsonNode GetDataAsJson()
        {
            return GetDataAsJson(configureOptions: null);
        }

        /// <summary>
        /// Tries to load the raw <see cref="Data"/> as a valid JSON node.
        /// </summary>
        /// <param name="configureOptions">The function to configure the additional options when loading the data preview as a JSON node.</param>
        public JsonNode GetDataAsJson(Action<DataPreviewJsonOptions> configureOptions)
        {
            var options = new DataPreviewJsonOptions();
            configureOptions?.Invoke(options);

            var previewAsJson = Data.ToString();
            try
            {
                JsonObject outputObj = ParseOutputNode(previewAsJson);
                PreviewHeader[] headers = ParseSchemeAsPreviewHeaders(outputObj);

                JsonNode data = ParseDataAsNode(headers, outputObj, options);
                return data;
            }
            catch (JsonException exception)
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as JSON as the run result data could not be parsed to JSON: '{exception.Message}', for data: {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures",
                    exception);
            }
        }

        private static PreviewHeader[] ParseSchemeAsPreviewHeaders(JsonObject outputObj)
        {
            if (!outputObj.TryGetPropertyValue("schema", out JsonNode headersNode)
                || headersNode is not JsonValue header
                || !header.ToString().StartsWith("output"))
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview expression as the headers are not available in the 'output.schema' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            var headersTxt = header.GetValue<string>();
            headersTxt = Regex.Replace(headersTxt, "^output\\(", string.Empty);
            headersTxt = headersTxt.Remove(headersTxt.Length - 1, 1);
            headersTxt = 
                headersTxt.Replace("\\n", "")
                          .Replace(", ", ",")
                          .Replace(" as string[]", " as string")
                          .Replace("{", "")
                          .Replace("}", "");

            if (Regex.IsMatch(headersTxt, ",( )*,"))
            {
                throw new JsonException(
                    $"Cannot load the content of the DataFactory preview as the headers are not considered in a valid format: {headersTxt}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }
                
            (int _, PreviewHeader[] parsed) = ParseSchemeAsPreviewHeaders(startIndex: 0, headersTxt);
            return parsed;
        }

        private static (int index, PreviewHeader[] parsed) ParseSchemeAsPreviewHeaders(int startIndex, string headersTxt)
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

                    return (i + 1, headers.ToArray());
                }

                if (ch == '(')
                {
                    (int currentIndex, PreviewHeader[] parsed) = ParseSchemeAsPreviewHeaders(i + 1, headersTxt);
                    bool endOfLine = currentIndex == headersTxt.Length;
                    if (endOfLine)
                    {
                        headers.Add(PreviewHeader.CreateAsObject(headerName.ToString(), parsed));
                        return (currentIndex, headers.ToArray());
                    }

                    bool markedAsInnerObject = headersTxt[currentIndex] is ')';
                    if (markedAsInnerObject)
                    {
                        headers.Add(PreviewHeader.CreateAsObject(headerName.ToString(), parsed));
                        return (currentIndex + 1, headers.ToArray());
                    }

                    bool markedAsArray = currentIndex + 2 <= headersTxt.Length && headersTxt[currentIndex..(currentIndex + 2)] == "[]";
                    if (markedAsArray)
                    {
                        headers.Add(PreviewHeader.CreateAsArray(headerName.ToString(), parsed));
                        headerName.Clear();
                        i = currentIndex + 1;
                        continue;
                    }

                    if (headersTxt[currentIndex] is ',')
                    {
                        headers.Add(PreviewHeader.CreateAsObject(headerName.ToString(), parsed));
                        headerName.Clear();
                        i = currentIndex;
                        continue;
                    }
                }

                if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-')
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

         private static JsonNode ParseDataAsNode(PreviewHeader[] headers, JsonObject outputObj, DataPreviewJsonOptions options)
         {
             JsonArray dataArray = ParseDataAsArray(outputObj);
             JsonNode[] results =
                dataArray.Where(elem => elem is JsonArray)
                         .Cast<JsonArray>()
                         .Select(arr => FillJsonDataFromHeaders(headers, arr, options))
                         .ToArray();

            return results.Length == 1 
                ? results[0] 
                : JsonSerializer.SerializeToNode(results);
        }

        private static JsonNode FillJsonDataFromHeaders(PreviewHeader[] headers, JsonArray dataArray, DataPreviewJsonOptions options)
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
                        result[headerName.Name] = ParseDirectValue(headerValue, options);
                        break;
                    
                    case PreviewDataType.Array when headerValue is JsonArray arr:
                        JsonNode[] elements = arr.Cast<JsonArray>().Select(elem => FillJsonDataFromHeaders(headerName.Children, elem, options)).ToArray();
                        result[headerName.Name] = JsonSerializer.SerializeToNode(elements);
                        break;
                    
                    case PreviewDataType.Object when headerValue is JsonArray inner:
                        JsonNode children = FillJsonDataFromHeaders(headerName.Children, inner, options);
                        result[headerName.Name] = JsonSerializer.SerializeToNode(children);
                        break;
                    
                    default:
                        throw new JsonException(
                            $"Cannot load the content of the DataFactory preview expression as the header and data is not representing the same types: {dataArray}, " +
                            $"consider parsing the raw run data yourself as this parsing only supports limited structures");
                }
            }

            return JsonSerializer.SerializeToNode(result);
        }

        private static JsonNode ParseDirectValue(JsonNode headerValue, DataPreviewJsonOptions options)
        {
            const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;
            if (float.TryParse(headerValue.ToString(), style, options.CultureInfo, out float numeric))
            {
                return numeric;
            }

            if (bool.TryParse(headerValue.ToString(), out bool flag))
            {
                return flag;
            }

            if (headerValue is JsonArray arr && arr.All(elem => elem is JsonValue))
            {
                return JsonSerializer.SerializeToNode(arr.Select(elem => ParseDirectValue(elem, options)).ToArray());
            }

            return headerValue;
        }

        /// <summary>
        /// Represents the available types the DataFactory data preview can handle.
        /// </summary>
        private enum PreviewDataType { DirectValue, Array, Object }

        /// <summary>
        /// Represents a single DataFactor data preview header, could be recursively contain other preview headers.
        /// </summary>
        private class PreviewHeader
        {
            private static readonly Regex DirectValueTrail = new(" as string$", RegexOptions.Compiled),
                                          ArrayOrObjectTrail = new(" as $", RegexOptions.Compiled);

            private PreviewHeader(string name, PreviewDataType type, PreviewHeader[] children)
            {
                Name = name;
                Type = type;
                Children = children;
            }

            public string Name { get; }
            public PreviewDataType Type { get; }
            public PreviewHeader[] Children { get; }

            public static PreviewHeader CreateAsValue(string headerName)
            {
                return new PreviewHeader(DirectValueTrail.Replace(headerName, ""), PreviewDataType.DirectValue, Array.Empty<PreviewHeader>());
            }

            public static PreviewHeader CreateAsArray(string headerName, PreviewHeader[] parsed)
            {
                return new PreviewHeader(ArrayOrObjectTrail.Replace(headerName, ""), PreviewDataType.Array, parsed);
            }

            public static PreviewHeader CreateAsObject(string headerName, PreviewHeader[] parsed)
            {
                return new PreviewHeader(ArrayOrObjectTrail.Replace(headerName, ""), PreviewDataType.Object, parsed);
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                return $"{Name}:{Type}";
            }
        }

        /// <summary>
        /// Tries to load the raw <see cref="Data"/> as a valid CSV table.
        /// </summary>
        /// <exception cref="CsvException">Thrown when the raw data could not be loaded as a valid CSV table.</exception>
        public CsvTable GetDataAsCsv()
        {
            return GetDataAsCsv(configureOptions: null);
        }

        /// <summary>
        /// Tries to load the raw <see cref="Data"/> as a valid CSV table.
        /// </summary>
        /// <param name="configureOptions">The function to configure the options to control the behavior when loading the <see cref="Data"/> contents.</param>
        /// <exception cref="CsvException">Thrown when the raw data could not be loaded as a valid CSV table.</exception>
        public CsvTable GetDataAsCsv(Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            var previewCsvAsJson = Data.ToString();
            try
            {
                JsonObject outputObj = ParseOutputNode(previewCsvAsJson);
                string[] headers = ParseSchemeAsCsvHeaders(outputObj);
                string[][] rows = ParseDataAsRows(outputObj);

                return DataFlowPreviewCsvTable.Load(headers, rows, options);
            }
            catch (JsonException exception)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as CSV as the run result data could not be parsed to JSON: '{exception.Message}' for data: {previewCsvAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures",
                    exception);
            }
        }

        private static JsonObject ParseOutputNode(string previewAsJson)
        {
            JsonNode json = JsonNode.Parse(previewAsJson);
            if (json is not JsonObject)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the content is loaded as 'null': {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string outputJson = json["output"]?.ToString();
            if (string.IsNullOrWhiteSpace(outputJson))
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the output is not available in the 'output' node: {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            JsonNode outputNode = JsonNode.Parse(outputJson.Replace("\n", ""));
            if (outputNode is not JsonObject outputObj)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the 'output' node value is not considered valid JSON: {previewAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            return outputObj;
        }

        private static string[] ParseSchemeAsCsvHeaders(JsonObject outputObj)
        {
            if (!outputObj.TryGetPropertyValue("schema", out JsonNode headersNode) 
                || headersNode is not JsonValue headersValue 
                || !headersValue.ToString().StartsWith("output"))
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the headers are not available in the 'output.schema' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string headersTxt = Regex.Replace(headersValue.GetValue<string>(), "^output\\(", string.Empty).TrimEnd(')');
            if (string.IsNullOrWhiteSpace(headersTxt))
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the headers are not available in the 'output.schema' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            headersTxt = Regex.Replace(headersTxt, " as string(\\[\\])?, ", ", ");
            headersTxt = Regex.Replace(headersTxt, " as string(\\[\\])?$", "");
            headersTxt = Regex.Replace(headersTxt, " as \\(", " (");
            headersTxt = Regex.Replace(headersTxt, "\\,", ",");

            var headers = new List<string>();
            var header = new StringBuilder();
            bool ignoreAsWithinParentheses = false;
            int depth = 0;

            for (int i = 0; i < headersTxt.Length; i++)
            {
                char ch = headersTxt[i];
                if (ch == ')')
                {
                    depth--;
                    ignoreAsWithinParentheses = false;
                }
                else if (ch == '(')
                {
                    depth++;
                    ignoreAsWithinParentheses = true;
                }

                if (ignoreAsWithinParentheses || depth > 0)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == ' ')
                {
                    header.Append(ch);
                }

                string h = header.ToString();
                bool leftover = h.Length > 0 && h.Any(x => x != ' ');
                bool eof = i == headersTxt.Length - 1;
                if (ch == ',' || (eof && leftover))
                {
                    headers.Add(h.Trim());
                    header.Clear();
                }
            }

            return headers.ToArray();
        }

        private static string[][] ParseDataAsRows(JsonObject outputObj)
        {
            JsonArray dataArr = ParseDataAsArray(outputObj);

            string AsCsvCell(string value)
            {
                return value.Replace("\\,", ",");
            }

            if (dataArr.All(n => n is JsonArray arr && arr.All(elem => elem is JsonValue)))
            {
                return dataArr.Select(n => n.AsArray().Select(x => AsCsvCell(x.GetValue<string>())).ToArray())
                              .ToArray();
            }

            return new[] { dataArr.Select(n => AsCsvCell(n.ToString())).ToArray() };
        }

        private static JsonArray ParseDataAsArray(JsonObject outputObj)
        {
            JsonNode dataNode = outputObj["data"];
            if (dataNode is not JsonArray dataArr)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as CSV as the rows are not available in the 'output.data' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            return dataArr;
        }

        /// <summary>
        /// Represents a specific DataFactory-version implementation of the <see cref="CsvTable"/>.
        /// </summary>
        private class DataFlowPreviewCsvTable : CsvTable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DataFlowPreviewCsvTable" /> class.
            /// </summary>
            private DataFlowPreviewCsvTable(
                string[] headerNames,
                CsvRow[] rows,
                string originalCsv,
                AssertCsvOptions options) : base(headerNames, rows, originalCsv, options) 
            {
            }

            /// <summary>
            /// Loads the raw <paramref name="headerNames"/> and <paramref name="rows"/> to a validly parsed <see cref="CsvTable"/>.
            /// </summary>
            internal static CsvTable Load(
                IEnumerable<string> headerNames,
                IEnumerable<IEnumerable<string>> rows,
                AssertCsvOptions options)
            {
                string[] headerNamesArr = headerNames.ToArray();
                string[][] rowsArr = rows.Select(r => r.ToArray()).ToArray();

                string csv = string.Join(options.NewLine,
                    rowsArr.Prepend(headerNamesArr.ToArray())
                           .Select(row => string.Join(options.Separator, row)));

                CsvRow[] parsed = ParseCsvRows(rowsArr, headerNamesArr, options);
                return new DataFlowPreviewCsvTable(headerNamesArr, parsed, csv, options);
            }
        }
    }
}