using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Xsl;
using Arcus.Testing.Failure;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents assertion-like functionality related to transforming XML documents with XSLT transformer contents.
    /// </summary>
    public static class AssertXslt
    {
        private const string TransformMethodName = $"{nameof(AssertXslt)}.{nameof(Transform)}",
                             LoadMethodName = $"{nameof(AssertXslt)}.{nameof(Load)}";

        /// <summary>
        /// Transforms the raw <paramref name="inputXml"/> with the given raw <paramref name="xsltTransformer"/> to a XML output.
        /// </summary>
        /// <param name="xsltTransformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The raw XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> or the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">
        ///     Thrown when the <paramref name="xsltTransformer"/> could not be successfully loaded into structured XSLT instance or the transformation was not successful.
        /// </exception>
        /// <exception cref="XmlException">Thrown when the <paramref name="inputXml"/> or output could not be successfully loaded into a structured XML document.</exception>
        public static string TransformToXml(string xsltTransformer, string inputXml)
        {
            return TransformToXml(xsltTransformer, inputXml, arguments: null);
        }

        /// <summary>
        /// Transforms the raw <paramref name="inputXml"/> with the given raw <paramref name="xsltTransformer"/> to a XML output.
        /// </summary>
        /// <param name="xsltTransformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The raw XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> or the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">
        ///     Thrown when the <paramref name="xsltTransformer"/> could not be successfully loaded into structured XSLT instance or the transformation was not successful.
        /// </exception>
        /// <exception cref="XmlException">Thrown when the <paramref name="inputXml"/> or output could not be successfully loaded into a structured XML document.</exception>
        public static string TransformToXml(string xsltTransformer, string inputXml, XsltArgumentList arguments)
        {
            ArgumentNullException.ThrowIfNull(xsltTransformer);
            ArgumentNullException.ThrowIfNull(inputXml);

            XslCompiledTransform transformer = Load(xsltTransformer);
            XmlNode input = AssertXml.Load(inputXml);

            XmlNode resultDoc = TransformToXml(transformer, input, arguments);
            return resultDoc.OuterXml;
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> XML document with the given XSLT <paramref name="transformer"/> to a XML output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="XmlException">Thrown when the output could not be successfully loaded into a structured XML document.</exception>
        public static XmlDocument TransformToXml(XslCompiledTransform transformer, XmlNode input)
        {
            return TransformToXml(transformer, input, arguments: null);
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> XML document with the given XSLT <paramref name="transformer"/> to a XML output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="XmlException">Thrown when the output could not be successfully loaded into a structured XML document.</exception>
        public static XmlDocument TransformToXml(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments)
        {
            ArgumentNullException.ThrowIfNull(transformer);
            ArgumentNullException.ThrowIfNull(input);

            try
            {
                string xml = Transform(transformer, input, arguments);
                return AssertXml.Load(xml);
            }
            catch (XsltException exception)
            {
                throw new XsltException(
                    ReportBuilder.ForMethod(TransformMethodName, $"cannot correctly transform XML input to XML due to a transformation failure: {exception.Message}")
                                 .AppendInput(input.OuterXml)
                                 .ToString(), exception);
            }
        }

        /// <summary>
        /// Transforms the raw <paramref name="inputXml"/> with the given raw <paramref name="xsltTransformer"/> to a JSON output.
        /// </summary>
        /// <param name="xsltTransformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The raw JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> or the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">
        ///     Thrown when the <paramref name="xsltTransformer"/> could not be successfully loaded into structured XSLT instance or the transformation was not successful.
        /// </exception>
        /// <exception cref="XmlException">Thrown when the <paramref name="inputXml"/> could not be successfully loaded into a structured XML document.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static string TransformToJson(string xsltTransformer, string inputXml)
        {
            return TransformToJson(xsltTransformer, inputXml, arguments: null);
        }

        /// <summary>
        /// Transforms the raw <paramref name="inputXml"/> with the given raw <paramref name="xsltTransformer"/> to a JSON output.
        /// </summary>
        /// <param name="xsltTransformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The raw JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> or the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">
        ///     Thrown when the <paramref name="xsltTransformer"/> could not be successfully loaded into structured XSLT instance or the transformation was not successful.
        /// </exception>
        /// <exception cref="XmlException">Thrown when the <paramref name="inputXml"/> could not be successfully loaded into a structured XML document.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static string TransformToJson(string xsltTransformer, string inputXml, XsltArgumentList arguments)
        {
            XslCompiledTransform transformer = Load(xsltTransformer);
            XmlNode input = AssertXml.Load(inputXml);

            JsonNode token = TransformToJson(transformer, input, arguments);
            return token.ToString();
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> with the given <paramref name="transformer"/> to a JSON output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode TransformToJson(XslCompiledTransform transformer, XmlNode input)
        {
            return TransformToJson(transformer, input, arguments: null);
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> with the given <paramref name="transformer"/> to a JSON output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode TransformToJson(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(transformer);

            try
            {
                string json = Transform(transformer, input, arguments);
                return AssertJson.Load(json);
            }
            catch (XsltException exception)
            {
                throw new XsltException(
                    ReportBuilder.ForMethod(TransformMethodName, $"cannot correctly transform XML input to JSON due to a transformation failure: {exception.Message}")
                                 .AppendInput(input.OuterXml)
                                 .ToString(), exception);
            }
        }

        /// <summary>
        /// Transforms the <paramref name="inputXml"/> with the given <paramref name="xsltTransformer"/> to a CSV output.
        /// </summary>
        /// <param name="xsltTransformer">The XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The CSV result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> or the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="CsvException">Thrown when the output could not be successfully loaded into a structured CSV table.</exception>
        public static string TransformToCsv(string xsltTransformer, string inputXml)
        {
            return TransformToCsv(xsltTransformer, inputXml, arguments: null);
        }

        /// <summary>
        /// Transforms the <paramref name="inputXml"/> with the given <paramref name="xsltTransformer"/>to a CSV output.
        /// </summary>
        /// <param name="xsltTransformer">The XSLT stylesheet that describes the transformation of the <paramref name="inputXml"/> XML contents.</param>
        /// <param name="inputXml">The XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The CSV result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> the <paramref name="inputXml"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="CsvException">Thrown when the output could not be successfully loaded into a structured CSV table.</exception>
        public static string TransformToCsv(string xsltTransformer, string inputXml, XsltArgumentList arguments)
        {
            XslCompiledTransform transformer = Load(xsltTransformer);
            XmlNode input = AssertXml.Load(inputXml);

            CsvTable csvTable = TransformToCsv(transformer, input, arguments);
            return csvTable.ToString();
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> with the given <paramref name="transformer"/> to a CSV output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The CSV result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="CsvException">Thrown when the output could not be successfully loaded into a structured CSV table.</exception>
        public static CsvTable TransformToCsv(XslCompiledTransform transformer, XmlNode input)
        {
            return TransformToCsv(transformer, input, arguments: null);
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> with the given <paramref name="transformer"/> to a CSV output.
        /// </summary>
        /// <param name="transformer">The XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The CSV result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="CsvException">Thrown when the output could not be successfully loaded into a structured CSV table.</exception>
        public static CsvTable TransformToCsv(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(transformer);

            try
            {
                string json = Transform(transformer, input, arguments);
                return AssertCsv.Load(json);
            }
            catch (XsltException exception)
            {
                throw new XsltException(
                    ReportBuilder.ForMethod(TransformMethodName, $"cannot correctly transform XML input to CSV due to a transformation failure: {exception.Message}")
                                 .AppendInput(input.OuterXml)
                                 .ToString(), exception);
            }
        }

        private static string Transform(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments = null)
        {
            using var txtWriter = new StringWriter();
            transformer.Transform(input, arguments, txtWriter);

            return txtWriter.ToString();
        }

        /// <summary>
        /// Loads the given raw <paramref name="xsltTransformer"/> XSLT input to a typed XSLT transformer instance.
        /// </summary>
        /// <param name="xsltTransformer">The raw input, representing a XSLT transformation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xsltTransformer"/> is <c>null</c>.</exception>>
        /// <exception cref="XsltException">Thrown when the <paramref name="xsltTransformer"/> could not be successfully loaded into structured XSLT instance.</exception>
        public static XslCompiledTransform Load(string xsltTransformer)
        {
            ArgumentNullException.ThrowIfNull(xsltTransformer);

            try
            {
                var transformer = new XslCompiledTransform(enableDebug: true);

                using var txtReader = new StringReader(xsltTransformer);
                using var xmlReader = XmlReader.Create(txtReader);
                transformer.Load(xmlReader);

                return transformer;
            }
            catch (XsltException exception)
            {
                throw new XsltException(
                    ReportBuilder.ForMethod(LoadMethodName, $"cannot correctly load the XSLT contents due to a deserialization failure: {exception.Message}")
                                 .AppendInput(xsltTransformer)
                                 .ToString(), exception);
            }
        }
    }
}
