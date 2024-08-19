using System;
using System.Collections.Generic;
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
                string[] headers = ParseSchemeAsHeaders(outputObj);
                string[][] rows = ParseDataAsRows(outputObj, options);

                return CsvTable.Load(headers, rows, options);
            }
            catch (JsonException exception)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as CSV as the run result data could not be parsed to JSON: '{exception.Message}' for data: {previewCsvAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures",
                    exception);
            }
        }

        private static JsonObject ParseOutputNode(string previewCsvAsJson)
        {
            JsonNode json = JsonNode.Parse(previewCsvAsJson);
            if (json is not JsonObject)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the content is loaded as 'null': {previewCsvAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string outputJson = json["output"]?.ToString();
            if (string.IsNullOrWhiteSpace(outputJson))
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the output is not available in the 'output' node: {previewCsvAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            JsonNode outputNode = JsonNode.Parse(outputJson);
            if (outputNode is not JsonObject outputObj)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as the 'output' node value is not considered valid JSON: {previewCsvAsJson}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            return outputObj;
        }

        private static string[] ParseSchemeAsHeaders(JsonObject outputObj)
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

            headersTxt = Regex.Replace(headersTxt, " as string, ", ", ");
            headersTxt = Regex.Replace(headersTxt, " as string$", "");
            headersTxt = Regex.Replace(headersTxt, " as \\(", " (");

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

        private static string[][] ParseDataAsRows(JsonObject outputObj, AssertCsvOptions options)
        {
            JsonNode dataNode = outputObj["data"];
            if (dataNode is not JsonArray dataArr)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as CSV as the rows are not available in the 'output.data' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            string AsCsvCell(string value)
            {
                const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;
                if (float.TryParse(value, style, options.CultureInfo, out _))
                {
                    return value;
                }

                return $"\"{value}\"";
            }

            if (dataArr.All(n => n is JsonArray arr && arr.All(elem => elem is JsonValue)))
            {
                return dataArr.Select(n => n.AsArray().Select(x => AsCsvCell(x.GetValue<string>())).ToArray())
                              .ToArray();
            }

            return new[] { dataArr.Select(n => AsCsvCell(n.ToString())).ToArray() };
        }
    }
}