using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using Arcus.Testing.Failure;
using Org.XmlUnit.Builder;
using Org.XmlUnit.Diff;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options when asserting on different XML documents in <see cref="AssertXml"/>.
    /// </summary>
    public class AssertXmlOptions
    {
        private readonly Collection<string> _ignoredNodeNames = new();

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
    }

    /// <summary>
    /// Represents assertion-like functionality related to comparing XML documents.
    /// </summary>
    public static class AssertXml
    {
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

            Diff diff =
                DiffBuilder.Compare(expected ?? throw new ArgumentNullException(nameof(expected)))
                           .WithTest(actual ?? throw new ArgumentNullException(nameof(actual)))
                           .CheckForSimilar()
                           .IgnoreComments()
                           .IgnoreWhitespace()
                           .WithNodeFilter(node => !options.IgnoredNodeNames.Contains(node.LocalName))
                           .Build();

            if (diff.HasDifferences())
            {
                string message = diff.FullDescription();
                string expectedXml = ReadXml(expected);
                string actualXml = ReadXml(actual);

                throw new EqualAssertionException(
                    ReportBuilder.ForMethod($"{nameof(AssertXml)}.{nameof(Equal)}", "expected and actual XML documents do not match")
                                     .AppendLine(message)
                                     .AppendDiff(expectedXml, actualXml)
                                     .ToString());
            }
        }

        private static string ReadXml(XmlDocument doc)
        {
            var output = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true };
            using var writer = XmlWriter.Create(output, settings);
            doc.Save(writer);

            return output.ToString();
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
                    ReportBuilder.ForMethod($"{nameof(AssertXml)}.{nameof(Load)}", $"cannot correctly load the XML contents due to a deserialization failure: {exception.Message}")
                                 .AppendInput(xml)
                                 .ToString(), exception);
            }
        }
    }
}
