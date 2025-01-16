using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using static Arcus.Testing.XmlDifferenceKind;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when asserting the order of XML elements in documents in the <see cref="AssertXml"/>.
    /// </summary>
    public enum AssertXmlOrder
    {
        /// <summary>
        /// Ignore the order of attributes when comparing documents (default).
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Take the order of attributes into account when comparing documents.
        /// </summary>
        Include
    }

    /// <summary>
    /// Represents the available options when asserting on different XML documents in <see cref="AssertXml"/>.
    /// </summary>
    public class AssertXmlOptions
    {
        private readonly Collection<string> _ignoredNodeNames = new();
        private AssertXmlOrder _order = AssertXmlOrder.Ignore;
        private int _maxInputCharacters = ReportBuilder.DefaultMaxInputCharacters;

        /// <summary>
        /// Adds a local element node name which will get ignored when comparing XML documents.
        /// </summary>
        /// <param name="localNodeName">The local name of the XML element that should be ignored.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="localNodeName"/> is blank.</exception>
        public AssertXmlOptions IgnoreNode(string localNodeName)
        {
            if (string.IsNullOrWhiteSpace(localNodeName))
            {
                throw new ArgumentException($"Requires a non-blank '{nameof(localNodeName)}' when adding an ignored local name of a XML element", nameof(localNodeName));
            }

            _ignoredNodeNames.Add(localNodeName);
            return this;
        }

        /// <summary>
        /// Gets the configured ignored local names of XML documents.
        /// </summary>
        internal IEnumerable<string> IgnoredNodeNames => _ignoredNodeNames;

        /// <summary>
        /// Gets or sets the type of order should be used when comparing XML documents.
        /// </summary>
        /// <remarks>
        ///     Only XML attributes can be configured to ignore their order as they are unique by their name,
        ///     XML elements are unique by their contents and cannot be ordered without custom code.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertXmlOrder Order
        {
            get => _order;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "XML order enumeration value is outside the bounds of the enumeration");
                }

                _order = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum characters of the expected and actual inputs should be written to the test output.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is lower than zero.</exception>
        public int MaxInputCharacters
        {
            get => _maxInputCharacters;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum input characters cannot be lower than zero");
                }

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
    /// Represents assertion-like functionality related to comparing XML documents.
    /// </summary>
    public static class AssertXml
    {
        private const string EqualMethodName = $"{nameof(AssertXml)}.{nameof(Equal)}",
                             LoadMethodName = $"{nameof(AssertXml)}.{nameof(Load)}";

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedXml"/> is the same as the <paramref name="actualXml"/>.
        /// </summary>
        /// <param name="expectedXml">The raw contents of the expected XML document.</param>
        /// <param name="actualXml">The raw contents of the actual XML document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedXml"/> or the <paramref name="actualXml"/> is <c>null</c>.</exception>
        /// <exception cref="XmlException">
        ///     Thrown when the <paramref name="expectedXml"/> or the <paramref name="actualXml"/> could not be successfully loaded into a structured XML document.
        /// </exception>
        public static void Equal(string expectedXml, string actualXml)
        {
            Equal(expectedXml ?? throw new ArgumentNullException(nameof(expectedXml)),
                  actualXml ?? throw new ArgumentNullException(nameof(actualXml)),
                  configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedXml"/> is the same as the <paramref name="actualXml"/>.
        /// </summary>
        /// <param name="expectedXml">The raw contents of the expected XML document.</param>
        /// <param name="actualXml">The raw contents of the actual XML document.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedXml"/> or the <paramref name="actualXml"/> is <c>null</c>.</exception>
        /// <exception cref="XmlException">
        ///     Thrown when the <paramref name="expectedXml"/> or the <paramref name="actualXml"/> could not be successfully loaded into a structured XML document.
        /// </exception>
        public static void Equal(string expectedXml, string actualXml, Action<AssertXmlOptions> configureOptions)
        {
            XmlDocument expected = Load(expectedXml);
            XmlDocument actual = Load(actualXml);

            Equal(expected, actual, configureOptions);
        }

        /// <summary>
        /// Verifies if the given <paramref name="expected"/> XML document is the same as the <paramref name="actual"/> XML document.
        /// </summary>
        /// <param name="expected">The expected XML document.</param>
        /// <param name="actual">The actual XML document.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> is <c>null</c>.</exception>
        public static void Equal(XmlDocument expected, XmlDocument actual)
        {
            Equal(expected ?? throw new ArgumentNullException(nameof(expected)),
                  actual ?? throw new ArgumentNullException(nameof(actual)),
                  configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given <paramref name="expected"/> XML document is the same as the <paramref name="actual"/> XML document.
        /// </summary>
        /// <param name="expected">The expected XML document.</param>
        /// <param name="actual">The actual XML document.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> is <c>null</c>.</exception>
        public static void Equal(XmlDocument expected, XmlDocument actual, Action<AssertXmlOptions> configureOptions)
        {
            var options = new AssertXmlOptions();
            configureOptions?.Invoke(options);

            XPathXmlNode expectedNode = XPathXmlNode.Root(expected, options);
            XPathXmlNode actualNode = XPathXmlNode.Root(actual, options);
            XmlDifference diff = CompareNode(expectedNode, actualNode, options);

            if (diff != null)
            {
                string expectedXml = diff.ExpectedNodeDiff != null && options.ReportScope is ReportScope.Limited ? diff.ExpectedNodeDiff : ReadXml(expected);
                string actualXml = diff.ActualNodeDiff != null && options.ReportScope is ReportScope.Limited ? diff.ActualNodeDiff : ReadXml(actual);

                string optionsDescription =
                    $"Options: {Environment.NewLine}" +
                    $"\t- attribute order: {options.Order}{Environment.NewLine}" +
                    $"\t- ignored node (local) names: [{string.Join(", ", options.IgnoredNodeNames)}]{Environment.NewLine}";

                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "expected and actual XML documents do not match")
                                 .AppendLine(diff.ToString())
                                 .AppendLine()
                                 .AppendLine(optionsDescription)
                                 .AppendDiff(expectedXml, actualXml,
                                     opt =>
                                     {
                                         opt.MaxInputCharacters = options.MaxInputCharacters;
                                         opt.Format = options.ReportFormat;
                                         opt.Scope = options.ReportScope;
                                     })
                                 .ToString());
            }
        }

        private static string ReadXml(XmlDocument doc)
        {
            try
            {
                var output = new StringBuilder();
                var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
                using var writer = XmlWriter.Create(output, settings);
                doc.WriteTo(writer);
                writer.Flush();

                return output.ToString();
            }
            catch (Exception exception) when (exception is InvalidOperationException or XmlException)
            {
                return doc.OuterXml;
            }
        }

        private static XmlDifference CompareNode(XPathXmlNode expected, XPathXmlNode actual, AssertXmlOptions options)
        {
            return expected.NodeType switch
            {
                XmlNodeType.Element => CompareElement(expected, actual, options),
                _ => null
            };
        }

        private static XmlDifference CompareElement(XPathXmlNode expected, XPathXmlNode actual, AssertXmlOptions options)
        {
            if (expected.NodeType != actual.NodeType)
            {
                return new(ActualOtherType, expected, actual)
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            if (expected.NamespaceUri != actual.NamespaceUri)
            {
                return new(ActualOtherNamespace, expected.NamespaceUri, actual.NamespaceUri, expected.Path)
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            if (expected.LocalName != actual.LocalName)
            {
                return new(ActualOtherName, expected, actual)
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            if (expected.Value != actual.Value)
            {
                string actualValue = XmlDifference.Describe(actual.Current.FirstChild);
                string expectedValue = XmlDifference.Describe(expected.Current.FirstChild);
                return new(ActualOtherValue, expectedValue, actualValue, expected.Path + "/text()")
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            XmlDifference attributeDiff = CompareAttributes(expected, actual, options);
            if (attributeDiff != null)
            {
                return attributeDiff;
            }

            XmlDifference childrenDiff = CompareChildNodes(expected, actual, options);
            return childrenDiff;
        }

        private static XmlDifference CompareChildNodes(XPathXmlNode expected, XPathXmlNode actual, AssertXmlOptions options)
        {
            if (expected.Children.Length != actual.Children.Length)
            {
                return new(DifferentElementLength, expected.Children.Length, actual.Children.Length, expected.Path + "/")
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            for (var index = 0; index < expected.Children.Length; index++)
            {
                XPathXmlNode expectedChild = expected.Children[index];
                XPathXmlNode actualChild = actual.Children[index];

                XmlDifference firstDifference = CompareNode(expectedChild, actualChild, options);
                if (firstDifference != null)
                {
                    return firstDifference;
                }
            }

            return null;
        }

        private static XmlDifference CompareAttributes(XPathXmlNode expected, XPathXmlNode actual, AssertXmlOptions options)
        {
            if (expected.Attributes.Length != actual.Attributes.Length)
            {
                return new XmlDifference(DifferentAttributeLength, expected.Attributes.Length, actual.Attributes.Length, expected.Path)
                {
                    ExpectedNodeDiff = expected.ToString(),
                    ActualNodeDiff = actual.ToString()
                };
            }

            for (var index = 0; index < expected.Attributes.Length; index++)
            {
                XmlAttribute expectedAttr = expected.Attributes[index];
                var path = $"{expected.Path}[@{expectedAttr.LocalName}]";

                XmlAttribute actualAttr =
                    options.Order is AssertXmlOrder.Ignore
                        ? Array.Find(actual.Attributes, a => a.LocalName == expectedAttr.LocalName && a.NamespaceURI == expectedAttr.NamespaceURI)
                        : actual.Attributes[index];

                if (actualAttr is null)
                {
                    return new(ActualMissingAttribute, XmlDifference.Describe(expectedAttr), actual: "", path)
                    {
                        ExpectedNodeDiff = expected.ToString(),
                        ActualNodeDiff = actual.ToString()
                    };
                }

                if (expectedAttr.NamespaceURI != actualAttr.NamespaceURI)
                {
                    return new(ActualOtherNamespace,
                        expectedAttr.NamespaceURI == string.Empty ? "no namespace" : expectedAttr.NamespaceURI,
                        actualAttr.NamespaceURI == string.Empty ? "no namespace" : actualAttr.NamespaceURI, path)
                    {
                        ExpectedNodeDiff = expected.ToString(),
                        ActualNodeDiff = actual.ToString()
                    };
                }

                if (expectedAttr.LocalName != actualAttr.LocalName)
                {
                    string actualName = XmlDifference.Describe(actualAttr);
                    string expectedName = XmlDifference.Describe(expectedAttr);
                    return new(ActualOtherName, expectedName, actualName, path)
                    {
                        ExpectedNodeDiff = expected.ToString(),
                        ActualNodeDiff = actual.ToString()
                    };
                }

                if (expectedAttr.Value != actualAttr.Value)
                {
                    string actualValue = XmlDifference.Describe(actualAttr.FirstChild);
                    string expectedValue = XmlDifference.Describe(expectedAttr.FirstChild);
                    return new(ActualOtherValue, expectedValue, actualValue, path)
                    {
                        ExpectedNodeDiff = expected.ToString(),
                        ActualNodeDiff = actual.ToString()
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the given raw <paramref name="xml"/> contents into a structured XML document.
        /// </summary>
        /// <param name="xml">The raw XML input contents.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xml"/> is <c>null</c>.</exception>
        /// <exception cref="XmlException">Thrown when the <paramref name="xml"/> could not be successfully loaded into a structured XML document.</exception>
        public static XmlDocument Load(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml ?? throw new ArgumentNullException(nameof(xml)));

                return doc;
            }
            catch (XmlException exception)
            {
                throw new XmlException(
                    ReportBuilder.ForMethod(LoadMethodName, $"cannot correctly load the XML contents due to a deserialization failure: {exception.Message}")
                                 .AppendInput(xml)
                                 .ToString(), exception);
            }
        }
    }

    /// <summary>
    /// Represents a <see cref="XmlElement"/> that is tracked with a XPath.
    /// </summary>
    internal sealed class XPathXmlNode
    {
        private readonly Node _node;

        private XPathXmlNode(Node node, AssertXmlOptions options)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));

            const string namespaceDefinition = "http://www.w3.org/2000/xmlns/"; // DevSkim: ignore DS137138
            Attributes =
                _node.Current.Attributes
                    ?.OfType<XmlAttribute>()
                    .Where(a => a.NamespaceURI != namespaceDefinition
                                && !options.IgnoredNodeNames.Contains(a.LocalName))
                    .ToArray() ?? Array.Empty<XmlAttribute>();
        }

        /// <summary>
        /// Gets the current XPath-tracked XML element.
        /// </summary>
        internal XmlNode Current => _node.Current;

        /// <summary>
        /// Gets the type of the current XML node.
        /// </summary>
        internal XmlNodeType NodeType => _node.Current.NodeType;

        /// <summary>
        /// Gets the local name (without namespace prefix) of the current XML node.
        /// </summary>
        internal string LocalName => _node.Current.LocalName;

        /// <summary>
        /// Gets the text value of the current node - <c>null</c> if not available.
        /// </summary>
        internal string Value => _node.Current.ChildNodes.OfType<XmlText>().Aggregate((string) null, (str, txt2) => str + txt2.Value);

        /// <summary>
        /// Gets the namespace under which the current XML node is defined.
        /// </summary>
        internal string NamespaceUri => _node.Current.NamespaceURI;

        /// <summary>
        /// Gets the XPath of the current XML node.
        /// </summary>
        internal string Path => _node.Path;

        /// <summary>
        /// Gets the sequence of attributes of the current XML element.
        /// </summary>
        internal XmlAttribute[] Attributes { get; }

        /// <summary>
        /// Gets the children XML nodes of the current XML node.
        /// </summary>
        internal XPathXmlNode[] Children => _node.Children;

        /// <summary>
        /// Creates a new XPath-tracked XML node based on a XML document.
        /// </summary>
        internal static XPathXmlNode Root(XmlDocument doc, AssertXmlOptions options)
        {
            if (doc?.DocumentElement is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (options.IgnoredNodeNames.Contains(doc.LocalName))
            {
                throw new InvalidOperationException(
                    $"Cannot configure {nameof(AssertXml)}.{nameof(AssertXml.Equal)} options with node name: '{doc.LocalName}' as this is the root of the XML contents, " +
                    $"which would mean that the entire document should be ignored for assertion");
            }

            return new XPathXmlNode(CreateNode(doc.DocumentElement, $"/{doc.DocumentElement.LocalName}", options), options);
        }

        private static Node CreateNode(XmlNode xml, string path, AssertXmlOptions options)
        {
            XmlElement[] children =
                xml.ChildNodes.OfType<XmlElement>()
                   .Where(n => !options.IgnoredNodeNames.Contains(n.LocalName))
                   .ToArray();

            return new Node
            {
                Current = xml,
                Path = path,
                Children = children.GroupBy(ch => ch.LocalName).SelectMany(group =>
                {
                    bool hasSingleNamedChild = group.Count() == 1;
                    return group.Select((ch, i) =>
                    {
                        string childPath = hasSingleNamedChild ? $"{path}/{ch.LocalName}" : $"{path}/{ch.LocalName}[{i}]";
                        return new XPathXmlNode(CreateNode(ch, childPath, options), options);
                    });
                }).ToArray()
            };
        }

        private sealed class Node
        {
            public XmlNode Current { get; init; }
            public string Path { get; init; }
            public XPathXmlNode[] Children { get; init; }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var xml = new StringBuilder();

            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
            using var writer = XmlWriter.Create(xml, settings);
            Current.WriteTo(writer);
            writer.Flush();

            var result = xml.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the type of <see cref="XmlDifference"/>.
    /// </summary>
    internal enum XmlDifferenceKind
    {
        DifferentElementLength,
        DifferentAttributeLength,

        ActualMissingAttribute,
        ActualOtherType,
        ActualOtherName,
        ActualOtherNamespace,
        ActualOtherValue,
    }

    /// <summary>
    /// Represents the single found difference between two XML contents.
    /// </summary>
    internal class XmlDifference
    {
        private readonly XmlDifferenceKind _kind;
        private readonly string _expected, _actual, _path;

        internal string ExpectedNodeDiff { get; init; }
        internal string ActualNodeDiff { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDifference" /> class.
        /// </summary>
        public XmlDifference(XmlDifferenceKind kind, XPathXmlNode expected, XPathXmlNode actual, string additionalPath = null)
        {
            _kind = kind;
            _expected = Describe(expected);
            _actual = Describe(actual);
            _path = expected.Path + additionalPath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDifference" /> class.
        /// </summary>
        public XmlDifference(XmlDifferenceKind kind, string expected, string actual, string path)
        {
            _kind = kind;
            _expected = expected ?? throw new ArgumentNullException(nameof(expected));
            _actual = actual ?? throw new ArgumentNullException(nameof(actual));
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDifference" /> class.
        /// </summary>
        public XmlDifference(XmlDifferenceKind kind, int expected, int actual, string path)
        {
            _kind = kind;
            _expected = expected.ToString();
            _actual = actual.ToString();
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        private static string Describe(XPathXmlNode node)
        {
            return Describe(node.Current);
        }

        /// <summary>
        /// Describe the <paramref name="node"/> in a humanly-readable text.
        /// </summary>
        internal static string Describe(XmlNode node)
        {
            if (node is null)
            {
                return "null";
            }

            return node.NodeType switch
            {
                XmlNodeType.Element => $"an element: <{node.LocalName}>",
                XmlNodeType.Attribute => $"an attribute: {node.LocalName}",
                XmlNodeType.Text => int.TryParse(node.Value, out int _)
                    ? $"a number: {node.Value}"
                    : $"a string: {node.Value}",
                _ => node.OuterXml
            };
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _kind switch
            {
                ActualMissingAttribute => $"actual XML misses {_expected} at {_path}",
                ActualOtherType => $"has {_actual} instead of {_expected} at {_path}",
                ActualOtherName => $"actual XML has a different name at {_path}, expected {_expected} while actual {_actual}",
                ActualOtherValue => $"actual XML has a different value at {_path}, expected {_expected} while actual {_actual}",
                ActualOtherNamespace => $"actual XML has a different namespace at {_path}, expected {_expected} while actual {_actual}",
                DifferentElementLength => $"has {_actual} element(s) instead of {_expected} at {_path}",
                DifferentAttributeLength => $"has {_actual} attribute(s) instead of {_expected} at {_path}",
                _ => throw new ArgumentOutOfRangeException("Unknown difference kind type", innerException: null)
            };
        }
    }
}