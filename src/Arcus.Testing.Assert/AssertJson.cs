using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Arcus.Testing.JsonDifferenceKind;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when asserting the order of JSON elements in documents in the <see cref="AssertJson"/>.
    /// </summary>
    public enum AssertJsonOrder
    {
        /// <summary>
        /// Ignore the order of elements (array values, object properties) when comparing documents (default).
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Take the order of elements (array values, object properties) into account when comparing documents.
        /// </summary>
        Include
    }

    /// <summary>
    /// Represents the available options when asserting on different JSON documents in <see cref="AssertJson"/>.
    /// </summary>
    public class AssertJsonOptions
    {
        private readonly Collection<string> _ignoredNodeNames = new();
        private AssertJsonOrder _order = AssertJsonOrder.Ignore;
        private int _maxInputCharacters = ReportBuilder.DefaultMaxInputCharacters;

        /// <summary>
        /// Adds a local element node name which will get ignored when comparing JSON documents.
        /// </summary>
        /// <param name="nodeName">The name of the JSON element that should be ignored.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="nodeName"/> is blank.</exception>
        public AssertJsonOptions IgnoreNode(string nodeName)
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                throw new ArgumentException($"Requires a non-blank '{nameof(nodeName)}' when adding an ignored name of a JSON node", nameof(nodeName));
            }

            _ignoredNodeNames.Add(nodeName);
            return this;
        }

        /// <summary>
        /// Gets the configured ignored names of JSON documents.
        /// </summary>
        internal IEnumerable<string> IgnoredNodeNames => _ignoredNodeNames;

        /// <summary>
        /// Gets or sets the type of order which should be used when comparing JSON documents.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertJsonOrder Order
        {
            get => _order;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "JSON order enumeration value is outside the bounds of the enumeration");
                }

                _order = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum characters of the expected and actual inputs should be written to the test output.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is lower than zero.</exception>
        public int MaxInputCharacters
        {
            get => _maxInputCharacters;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
                _maxInputCharacters = value;
            }
        }

        /// <summary>
        /// Gets or sets the position in the input document that should be included in the failure report (default: <see cref="ReportScope.Limited"/>).
        /// </summary>
        public ReportScope ReportScope { get; set; } = ReportScope.Limited;

        /// <summary>
        /// Gets or sets the format in which the different input documents will be shown in the failure report (default: <see cref="ReportFormat.Horizontal"/>).
        /// </summary>
        public ReportFormat ReportFormat { get; set; } = ReportFormat.Horizontal;
    }

    /// <summary>
    /// Represents assertion-like functionality related to comparing JSON documents.
    /// </summary>
    public static class AssertJson
    {
        private const string JsonRootPath = "$",
                             EqualMethodName = $"{nameof(AssertJson)}.{nameof(Equal)}",
                             LoadMethodName = $"{nameof(AssertJson)}.{nameof(Load)}";

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedJson"/> is the same as the <paramref name="actualJson"/>.
        /// </summary>
        /// <param name="expectedJson">The raw contents of the expected JSON document.</param>
        /// <param name="actualJson">The raw contents of the actual JSON document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedJson"/> or the <paramref name="actualJson"/> is <c>null</c>.</exception>
        /// <exception cref="JsonException">
        ///     Thrown when the <paramref name="expectedJson"/> or the <paramref name="actualJson"/> could not be successfully loaded into a structured JSON document.
        /// </exception>
        public static void Equal(string expectedJson, string actualJson)
        {
            Equal(expectedJson, actualJson, configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedJson"/> is the same as the <paramref name="actualJson"/>.
        /// </summary>
        /// <param name="expectedJson">The raw contents of the expected JSON document.</param>
        /// <param name="actualJson">The raw contents of the actual JSON document.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedJson"/> or the <paramref name="actualJson"/> is <c>null</c>.</exception>
        /// <exception cref="JsonException">
        ///     Thrown when the <paramref name="expectedJson"/> or the <paramref name="actualJson"/> could not be successfully loaded into a structured JSON document.
        /// </exception>
        public static void Equal(string expectedJson, string actualJson, Action<AssertJsonOptions> configureOptions)
        {
            JsonNode expected = Load(expectedJson);
            JsonNode actual = Load(actualJson);

            Equal(expected, actual, configureOptions);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expected"/> is the same as the <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">The raw contents of the expected JSON document.</param>
        /// <param name="actual">The raw contents of the actual JSON document.</param>
        public static void Equal(JsonNode expected, JsonNode actual)
        {
            Equal(expected, actual, configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expected"/> is the same as the <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">The raw contents of the expected JSON document.</param>
        /// <param name="actual">The raw contents of the actual JSON document.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        public static void Equal(JsonNode expected, JsonNode actual, Action<AssertJsonOptions> configureOptions)
        {
            var options = new AssertJsonOptions();
            configureOptions?.Invoke(options);

            JsonDifference diff = CompareJsonRoot(expected, actual, options);
            if (diff != null)
            {
                string expectedJson = diff.ExpectedNodeDiff != null && options.ReportScope is ReportScope.Limited ? diff.ExpectedNodeDiff : expected?.ToString() ?? "null";
                string actualJson = diff.ActualNodeDiff != null && options.ReportScope is ReportScope.Limited ? diff.ActualNodeDiff : actual?.ToString() ?? "null";

                string optionsDescription =
                    $"Options: {Environment.NewLine}" +
                    $"\t- node order {options.Order}{Environment.NewLine}" +
                    $"\t- ignored node names: [{string.Join(", ", options.IgnoredNodeNames)}]{Environment.NewLine}";

                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "expected and actual JSON contents do not match")
                                 .AppendLine(diff.ToString())
                                 .AppendLine()
                                 .AppendLine(optionsDescription)
                                 .AppendDiff(expectedJson, actualJson, opt =>
                                 {
                                     opt.MaxInputCharacters = options.MaxInputCharacters;
                                     opt.Format = options.ReportFormat;
                                     opt.Scope = options.ReportScope;
                                 })
                                 .ToString());
            }
        }

        private static JsonDifference CompareJsonRoot(JsonNode expected, JsonNode actual, AssertJsonOptions options)
        {
            if (actual == expected)
            {
                return null;
            }

            if (actual is null)
            {
                return new JsonDifference(ActualIsNull, JsonRootPath);
            }

            if (expected is null)
            {
                return new JsonDifference(ExpectedIsNull, JsonRootPath);
            }

            return CompareJsonNode(expected, actual, options);
        }

        private static JsonDifference CompareJsonNode(JsonNode expected, JsonNode actual, AssertJsonOptions options)
        {
            return actual switch
            {
                JsonArray actualArray => CompareJsonArray(expected, actualArray, options),
                JsonObject actualObject => CompareJsonObject(expected, actualObject, options),
                JsonValue actualValue => CompareJsonValue(expected, actualValue),
                null => expected is null ? null : new JsonDifference(ActualIsNull, expected.GetPath())
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = "null"
                },
                _ => null
            };
        }

        private static JsonDifference CompareJsonArray(JsonNode expected, JsonArray actualArray, AssertJsonOptions options)
        {
            if (expected is not JsonArray expectedArray)
            {
                return new(ActualOtherType, expected, actualArray);
            }

            JsonNode[] actualChildren = actualArray.ToArray();
            JsonNode[] expectedChildren = expectedArray.ToArray();

            if (actualChildren.Length != expectedChildren.Length)
            {
                return new(DifferentLength, expectedArray.GetPath(), actualChildren.Length, expectedChildren.Length)
                {
                    ExpectedNodeDiff = expectedArray.ToString(),
                    ActualNodeDiff = actualArray.ToString(),
                };
            }

            if (options.Order is AssertJsonOrder.Ignore)
            {
                if (expectedChildren.OfType<JsonValue>().Any())
                {
                    expectedChildren = expectedChildren.OrderBy(ch => ch.ToString()).ToArray();
                    actualChildren = actualChildren.OrderBy(ch => ch.ToString()).ToArray(); 
                }
                else if (expectedChildren.OfType<JsonObject>().Any())
                {
                    if (Array.Exists(actualChildren, ch => ch is not JsonObject))
                    {
                        return new(ActualOtherType, expected, actualArray);
                    }

                    Dictionary<string, JsonNode>[] expectedObjects = CreatedOrderedDictionaries(expectedChildren, options);
                    Dictionary<string, JsonNode>[] actualObjects = CreatedOrderedDictionaries(actualChildren, options);

                    return expectedObjects.Select((expectedObject, index) => CompareJsonObject(expectedObject, actualObjects[index], options))
                                          .FirstOrDefault(firstDifference => firstDifference != null);
                }
            }

            return actualChildren.Select((actualChild, index) => CompareJsonNode(expectedChildren[index], actualChild, options))
                                 .FirstOrDefault(firstDifference => firstDifference != null);
        }

        private static JsonDifference CompareJsonObject(JsonNode expected, JsonObject actual, AssertJsonOptions options)
        {
            if (expected is not JsonObject expectedObject)
            {
                return new(ActualOtherType, expected, actual);
            }

            Dictionary<string, JsonNode> expectedDir = CreateDictionary(expectedObject, options);
            Dictionary<string, JsonNode> actualDir = CreateDictionary(actual, options);

            return CompareJsonObject(expectedDir, actualDir, options);
        }

        private static JsonDifference CompareJsonObject(Dictionary<string, JsonNode> expectedDir, Dictionary<string, JsonNode> actualDir, AssertJsonOptions options)
        {
            var serializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            if (TryGetValue(expectedDir, key => !actualDir.ContainsKey(key), out JsonNode missingActualPair))
            {
                return new(ActualMissesProperty, missingActualPair.GetPath())
                {
                    ExpectedNodeDiff = JsonSerializer.Serialize(expectedDir, serializerOptions),
                    ActualNodeDiff = JsonSerializer.Serialize(actualDir, serializerOptions)
                };
            }

            if (TryGetValue(actualDir, key => !expectedDir.ContainsKey(key), out JsonNode missingExpectedPair))
            {
                return new(ExpectedMissesProperty, missingExpectedPair.GetPath())
                {
                    ExpectedNodeDiff = JsonSerializer.Serialize(expectedDir, serializerOptions),
                    ActualNodeDiff = JsonSerializer.Serialize(actualDir, serializerOptions)
                };
            }

            foreach (KeyValuePair<string, JsonNode> expectedPair in expectedDir)
            {
                JsonNode actualValue = actualDir[expectedPair.Key];
                JsonDifference firstDifference = CompareJsonNode(expectedPair.Value, actualValue, options);

                if (firstDifference != null)
                {
                    return firstDifference;
                }
            }

            return null;
        }

        private static Dictionary<string, JsonNode>[] CreatedOrderedDictionaries(JsonNode[] children, AssertJsonOptions options)
        {
            return children.Cast<JsonObject>()
                           .Select(ch => CreateDictionary(ch, options))
                           .OrderBy(ch => JsonSerializer.Serialize(ch))
                           .ToArray();
        }

        private static Dictionary<string, JsonNode> CreateDictionary(JsonObject node, AssertJsonOptions options)
        {
            try
            {
                var properties = node.Where(n => !options.IgnoredNodeNames.Contains(n.Key));

                if (options.Order is AssertJsonOrder.Ignore)
                {
                    properties = properties.OrderByDescending(n => n.Value?.GetType().Name)
                                           .ThenBy(p => p.Key);
                }

                return properties.ToDictionary(n => n.Key, n => n.Value, StringComparer.InvariantCultureIgnoreCase);
            }
            /* Context: Microsoft's System.Text.Json deserialization allows duplicate JSON keys upon deserialization, but not during navigation,
                hence the need to catch this if it happens since we can't be sure upon loading the JSON contents. */
            catch (ArgumentException exception)
            {
                throw new JsonException(
                    ReportBuilder.ForMethod(EqualMethodName, $"cannot load the JSON contents to a dictionary to to invalid keys, please use unique JSON keys: {exception.Message}")
                                 .AppendInput(node.ToString())
                                 .ToString(), exception);
            }
        }

        private static bool TryGetValue(IDictionary<string, JsonNode> dictionary, Func<string, bool> keyPredicate, out JsonNode value)
        {
            value = dictionary.FirstOrDefault(pair => keyPredicate(pair.Key)).Value;
            return value != null;
        }

        private static JsonDifference CompareJsonValue(JsonNode expected, JsonValue actualValue)
        {
            if (expected is not JsonValue expectedValue)
            {
                return new(ActualOtherType, expected, actualValue);
            }

#if  NET8_0
            if (actualValue.GetValueKind() != expectedValue.GetValueKind())
            {
                return new(ActualOtherType, expectedValue, actualValue);
            }

            bool identical = expectedValue.GetValueKind() switch
            {
                JsonValueKind.String
                    or JsonValueKind.False
                    or JsonValueKind.True => JsonNode.DeepEquals(expectedValue, actualValue),

                JsonValueKind.Number => expectedValue.TryGetValue(out float expectedValue1)
                                        && actualValue.TryGetValue(out float actualValue1)
                                        && expectedValue1.Equals(actualValue1),
                _ => false
            };

#elif NET6_0
            bool identicalFloats =
                expectedValue.TryGetValue(out float expectedFloat)
                && actualValue.TryGetValue(out float actualFloat)
                && expectedFloat.Equals(actualFloat);
            
            bool identicalBool =
                expectedValue.TryGetValue(out bool expectedBool)
                && actualValue.TryGetValue(out bool actualBool)
                && expectedBool == actualBool;

            bool identicalString =
                expectedValue.TryGetValue(out string expectedString)
                && actualValue.TryGetValue(out string actualString)
                && expectedString.Equals(actualString);

            bool identical = identicalFloats || identicalBool || identicalString;
#endif
            if (!identical)
            {
                return new(ActualOtherValue, expectedValue, actualValue)
                {
                    ExpectedNodeDiff = expectedValue.Parent?.ToString(),
                    ActualNodeDiff = actualValue.Parent?.ToString()
                };
            }

            return null;
        }

        /// <summary>
        /// Loads the given raw <paramref name="json"/> contents into a structured JSON document.
        /// </summary>
        /// <param name="json">The raw JSON input contents.</param>
        /// <returns>The loaded JSON node; or <c>null</c> in case of a Null JSON node.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="json"/> is <c>null</c>.</exception>
        /// <exception cref="JsonException">Thrown when the <paramref name="json"/> could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode Load(string json)
        {
            return Load(json, configureNodeOptions: null, configureDocOptions: null);
        }

        /// <summary>
        /// Loads the given raw <paramref name="json"/> contents into a structured JSON document.
        /// </summary>
        /// <param name="json">The raw JSON input contents.</param>
        /// <param name="configureNodeOptions">The function to configure the options to control the node node behavior after parsing.</param>
        /// <param name="configureDocOptions">The function to configure the options to control the document behavior during parsing.</param>
        /// <returns>The loaded JSON node; or <c>null</c> in case of a Null JSON node.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="json"/> is <c>null</c>.</exception>
        /// <exception cref="JsonException">Thrown when the <paramref name="json"/> could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode Load(string json, Action<JsonNodeOptions> configureNodeOptions, Action<JsonDocumentOptions> configureDocOptions)
        {
            var nodeOptions = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
            configureNodeOptions?.Invoke(nodeOptions);

            var docOptions = new JsonDocumentOptions();
            configureDocOptions?.Invoke(docOptions);

            try
            {
                return JsonNode.Parse(json ?? throw new ArgumentNullException(nameof(json)), nodeOptions, docOptions);
            }
            catch (JsonException exception)
            {
                throw new JsonException(
                    ReportBuilder.ForMethod(LoadMethodName, $"cannot correctly load the JSON contents due to a deserialization failure: {exception.Message}")
                                 .AppendInput(json)
                                 .ToString(), exception);
            }
        }
    }

    /// <summary>
    /// Represents the single found difference between two JSON contents.
    /// </summary>
    internal class JsonDifference
    {
        private readonly JsonDifferenceKind _kind;
        private readonly string _path;
        private readonly object _actual, _expected;

        internal string ExpectedNodeDiff { get; init; }
        internal string ActualNodeDiff { get; init; }

        internal JsonDifference(JsonDifferenceKind kind, JsonNode expected, JsonNode actual)
            : this(kind, expected?.GetPath() ?? actual?.GetPath() ?? "<not-available>", expected: Describe(expected), actual: Describe(actual))
        {
            ExpectedNodeDiff = expected?.ToString() ?? "null";
            ActualNodeDiff = actual?.ToString() ?? "null";
        }

        internal JsonDifference(JsonDifferenceKind kind, string path, object actual, object expected)
            : this(kind, path)
        {
            _actual = actual;
            _expected = expected;
        }

        internal JsonDifference(JsonDifferenceKind kind, string path)
        {
            _kind = kind;
            _path = path;
        }

        private static string Describe(JsonNode node)
        {
            if (node is null)
            {
                return "type null";
            }

            string nodeTxt = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
#if NET8_0

            JsonValueKind type = node.GetValueKind();
            return type switch
            {
                JsonValueKind.Undefined => "type none",
                JsonValueKind.Object => $"an object: {nodeTxt}",
                JsonValueKind.Array => $"an array: {node}",
                JsonValueKind.String => $"a string: {node}",
                JsonValueKind.Number => $"a number: {node}",
                JsonValueKind.True => "true boolean",
                JsonValueKind.False => "false boolean",
                JsonValueKind.Null => "type null",
                _ => throw new ArgumentOutOfRangeException(nameof(node), type, "Unknown JSON value type")
            };
#elif NET6_0
            string DescribeJsonValue(JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue(out float _))
                {
                    return $"a number: {node}";
                }

                if (jsonValue.TryGetValue(out bool b))
                {
                    return $"{b} boolean";
                }

                return $"a string: {node}";
            }

            return node switch
            {
                JsonObject _ => $"an object: {nodeTxt}",
                JsonArray _ => $"an array: {node}",
                JsonValue value => DescribeJsonValue(value),
                _ => throw new ArgumentOutOfRangeException(nameof(node), node, "Unknown JSON value type")
            };
#endif
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _kind switch
            {
                ActualIsNull => $"actual JSON is null at {_path}",
                ExpectedIsNull => $"expected JSON is null at {_path}",
                ActualOtherType => $"actual JSON has a different type at {_path}, expected {_expected} while actual {_actual}",
                ActualOtherValue => $"actual JSON has a different value at {_path}, expected {_expected} while actual {_actual}",
                DifferentLength => $"actual JSON has {_actual} elements instead of {_expected} at {_path}",
                ActualMissesProperty => $"actual JSON misses property at {_path}",
                ExpectedMissesProperty => $"expected JSON misses property at {_path}",
                ActualMissesElement => $"actual JSON misses expected JSON element {_path}",
                _ => throw new ArgumentOutOfRangeException("Unknown difference kind type", innerException: null),
            };
        }
    }

    /// <summary>
    /// Represents the type of <see cref="JsonDifference"/>.
    /// </summary>
    internal enum JsonDifferenceKind
    {
        ActualIsNull,
        ExpectedIsNull,
        ActualOtherType,
        ActualOtherValue,
        ActualMissesProperty,
        ExpectedMissesProperty,
        ActualMissesElement,
        DifferentLength,
    }
}
