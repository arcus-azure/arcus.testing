using System;
using System.Collections.Generic;
using System.Linq;
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
                string[][] rows = ParseDataAsRows(outputObj);

                string csv = string.Join(options.NewLine, 
                    rows.Prepend(headers.ToArray())
                        .Select(row => string.Join(options.Separator, row)));

                if (string.IsNullOrWhiteSpace(csv))
                {
                    throw new CsvException(
                        "Cannot load the content of the DataFactory preview expression as CSV as there were no headers and rows available, resulting in a blank value, " +
                        $"consider parsing the raw run data yourself as this parsing only supports limited structures");
                }

                return AssertCsv.Load(csv, configureOptions);
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
            string header = "";
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
                    header += ch;
                }

                bool leftover = !string.IsNullOrWhiteSpace(header);
                bool eof = i == headersTxt.Length - 1;
                if (ch == ',' || (eof && leftover))
                {
                    headers.Add(header.Trim());
                    header = "";
                }
            }

            return headers.ToArray();
        }

        private static string[][] ParseDataAsRows(JsonObject outputObj)
        {
            JsonNode dataNode = outputObj["data"];
            if (dataNode is not JsonArray dataArr)
            {
                throw new CsvException(
                    $"Cannot load the content of the DataFactory preview expression as CSV as the rows are not available in the 'output.data' node: {outputObj}, " +
                    $"consider parsing the raw run data yourself as this parsing only supports limited structures");
            }

            if (dataArr.All(n => n is JsonArray arr && arr.All(elem => elem is JsonValue)))
            {
                return dataArr.Select(n => n.AsArray().Select(x => x.GetValue<string>()).ToArray())
                              .ToArray();
            }

            return new[] { dataArr.Select(n => n.ToString()).ToArray() };
        }
    }
}