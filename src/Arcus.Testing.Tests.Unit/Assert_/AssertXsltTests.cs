using System.IO;
using System.Xml.Xsl;
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

        [Fact]
        public void Load_WithInvalidXml_ThrowsWithDescription()
        {
            var exception = Assert.ThrowsAny<XsltException>(() => AssertXslt.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertXslt), exception.Message);
            Assert.Contains("XSLT contents", exception.Message);
        }
    }
}
