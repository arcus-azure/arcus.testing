﻿using System;
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
        public static string TransformXml(string xsltTransformer, string inputXml)
        {
            return TransformXml(
                xsltTransformer ?? throw new ArgumentNullException(nameof(xsltTransformer)), 
                inputXml ?? throw new ArgumentNullException(nameof(inputXml)), 
                arguments: null);
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
        public static string TransformXml(string xsltTransformer, string inputXml, XsltArgumentList arguments)
        {
            XslCompiledTransform transformer = Load(xsltTransformer);
            XmlNode input = AssertXml.Load(inputXml);

            XmlNode resultDoc = TransformXml(transformer, input, arguments);
            return resultDoc.OuterXml;
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> XML document with the given XSLT <paramref name="transformer"/> to a XML output.
        /// </summary>
        /// <param name="transformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="XmlException">Thrown when the output could not be successfully loaded into a structured XML document.</exception>
        public static XmlDocument TransformXml(XslCompiledTransform transformer, XmlNode input)
        {
            return TransformXml(
                transformer ?? throw new ArgumentNullException(nameof(transformer)), 
                input ?? throw new ArgumentNullException(nameof(input)),
                arguments: null);
        }

        /// <summary>
        /// Transforms the <paramref name="input"/> XML document with the given XSLT <paramref name="transformer"/> to a XML output.
        /// </summary>
        /// <param name="transformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The XML result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="XmlException">Thrown when the output could not be successfully loaded into a structured XML document.</exception>
        public static XmlDocument TransformXml(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments)
        {
            if (transformer is null)
            {
                throw new ArgumentNullException(nameof(transformer));
            }

            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

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
        public static string TransformJson(string xsltTransformer, string inputXml)
        {
            return TransformJson(
                xsltTransformer ?? throw new ArgumentNullException(nameof(xsltTransformer)),
                inputXml ?? throw new ArgumentNullException(nameof(inputXml)),
                arguments: null);
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
        public static string TransformJson(string xsltTransformer, string inputXml, XsltArgumentList arguments)
        {
            XslCompiledTransform transformer = Load(xsltTransformer ?? throw new ArgumentNullException(nameof(xsltTransformer)));
            XmlNode input = AssertXml.Load(inputXml ?? throw new ArgumentNullException(nameof(inputXml)));

            JsonNode token = TransformJson(transformer, input, arguments);
            return token.ToString();
        }

        /// <summary>
        /// Transforms the raw <paramref name="input"/> with the given raw <paramref name="transformer"/> to a JSON output.
        /// </summary>
        /// <param name="transformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <returns>The raw JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode TransformJson(XslCompiledTransform transformer, XmlNode input)
        {
            return TransformJson(
                transformer ?? throw new ArgumentNullException(nameof(transformer)),
                input ?? throw new ArgumentNullException(nameof(input)),
                arguments: null);
        }

        /// <summary>
        /// Transforms the raw <paramref name="input"/> with the given raw <paramref name="transformer"/> to a JSON output.
        /// </summary>
        /// <param name="transformer">The raw XSLT stylesheet that describes the transformation of the <paramref name="input"/> XML contents.</param>
        /// <param name="input">The raw XML input contents, subject to the XSLT transformation.</param>
        /// <param name="arguments">The additional run-time arguments for the XSLT transformation.</param>
        /// <returns>The raw JSON result of the XSLT stylesheet transformation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="transformer"/> or the <paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="XsltException">Thrown when the XSLT transformation was not successful.</exception>
        /// <exception cref="JsonException">Thrown when the output could not be successfully loaded into a structured JSON document.</exception>
        public static JsonNode TransformJson(XslCompiledTransform transformer, XmlNode input, XsltArgumentList arguments)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (transformer is null)
            {
                throw new ArgumentNullException(nameof(transformer));
            }

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
            try
            {
                var transformer = new XslCompiledTransform(enableDebug: true);

                using var txtReader = new StringReader(xsltTransformer ?? throw new ArgumentNullException(nameof(xsltTransformer)));
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
