using System.IO;
using System.Text.Json;
using System.Xml;
using System.Xml.Xsl;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertXsltTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void TransformXml_ToXml_Succeeds()
        {
            // Arrange
            string sampleName = "xslt-transform.xml-xml.sample";
            string xslt = ReadResourceFileByName($"{sampleName}.transformer.xslt");

            string input = ReadResourceFileByName($"{sampleName}.input.xml");

            // Act
            string actual = TransformXml(xslt, input);

            // Assert
            string expected = ReadResourceFileByName($"{sampleName}.output.xml");
            AssertXml.Equal(expected, actual);
        }

        [Fact]
        public void TransformXml_ToJson_Succeeds()
        {
            // Arrange
            string sampleName = "xslt-transform.xml-json.sample";
            string xslt = ReadResourceFileByName($"{sampleName}.transformer.xslt");
            string input = ReadResourceFileByName($"{sampleName}.input.xml");
            string expected = ReadResourceFileByName($"{sampleName}.output.json");

            // Act
            string actual = TransformJson(xslt, input);

            // Assert
            AssertJson.Equal(expected, actual);
        }

        [Fact]
        public void TransformXml_ToCsv_Succeeds()
        {
            // Arrange
            string sampleName = "xslt-transform.xml-csv.sample";
            string xslt = ReadResourceFileByName($"{sampleName}.transformer.xslt");
            string input = ReadResourceFileByName($"{sampleName}.input.xml");
            string expected = ReadResourceFileByName($"{sampleName}.output.csv");

            // Act
            string actual = TransformCsv(xslt, input);

            // Assert
            AssertCsv.Equal(expected, actual);
        }

        [Fact]
        public void TransformJson_WithInvalidOutput_FailsWithDescription()
        {
            // Arrange
            string xslt = 
                "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
                "<xsl:template match=\"/\"><root/></xsl:template></xsl:stylesheet>";

            string input = TestXml.Generate().ToString();

            // Act / Assert
            var exception = Assert.ThrowsAny<JsonException>(() => TransformJson(xslt, input));
            Assert.Contains(nameof(AssertJson), exception.Message);
            Assert.Contains("deserialization failure", exception.Message);
        }

        [Fact]
        public void TransformXml_WithInvalidOutput_FailsWithDescription()
        {
            // Arrange
            string xslt = 
                "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
                "<xsl:template match=\"/\">{ \"root\": [] }</xsl:template></xsl:stylesheet>";

            string input = TestXml.Generate().ToString();

            // Act / Assert
            var exception = Assert.ThrowsAny<XmlException>(() => TransformXml(xslt, input));
            Assert.Contains(nameof(AssertXml), exception.Message);
            Assert.Contains("deserialization failure", exception.Message);
        }

        [Fact]
        public void TransformCsv_WithInvalidOutput_FailsWithDescription()
        {
            // Arrange
            string xslt = 
                "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
                "<xsl:template match=\"/\">a;b;c\n1;3</xsl:template></xsl:stylesheet>";

            string input = TestXml.Generate().ToString();

            // Act / Assert
            var exception = Assert.ThrowsAny<CsvException>(() => TransformCsv(xslt, input));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.Contains("load", exception.Message);
            Assert.Contains("CSV contents", exception.Message);
        }

        [Fact]
        public void Transform_WithInvalidTransformation_FailsWithDescription()
        {
            // Arrange
            string xslt = 
                "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
                    "<xsl:template match=\"/\"><xsl:message terminate=\"yes\">NotImplementedException</xsl:message></xsl:template></xsl:stylesheet>";

            string input = TestXml.Generate().ToString();

            // Act / Assert
            Assert.All(new[] { TransformXml, TransformJson, TransformXml }, transform =>
            {
                var exception = Assert.ThrowsAny<XsltException>(() => transform(xslt, input));

                Assert.Contains(nameof(AssertXslt), exception.Message);
                Assert.Contains("transformation failure", exception.Message);
            });
        }

        private static string ReadResourceFileByName(string fileName)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), nameof(Assert_), "Resources");
            string filePath = Path.Combine(directoryPath, fileName);

            return File.ReadAllText(filePath);
        }

        private static string TransformXml(string xslt, string xml)
        {
            if (Bogus.Random.Bool())
            {
                return AssertXslt.TransformXml(xslt, xml);
            }

            return AssertXslt.TransformXml(AssertXslt.Load(xslt), AssertXml.Load(xml)).OuterXml;
        }

        private static string TransformJson(string xslt, string xml)
        {
            if (Bogus.Random.Bool())
            {
                return AssertXslt.TransformJson(xslt, xml);
            }

            return AssertXslt.TransformJson(AssertXslt.Load(xslt), AssertXml.Load(xml)).ToString();
        }

        private static string TransformCsv(string xslt, string xml)
        {
            if (Bogus.Random.Bool())
            {
                return AssertXslt.TransformCsv(xslt, xml);
            }

            return AssertXslt.TransformCsv(AssertXslt.Load(xslt), AssertXml.Load(xml)).ToString();
        }

        [Fact]
        public void Load_WithInvalidXml_ThrowsWithDescription()
        {
            var exception = Assert.ThrowsAny<XsltException>(() => AssertXslt.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertXslt), exception.Message);
            Assert.Contains("XSLT contents", exception.Message);
        }
    }
}
