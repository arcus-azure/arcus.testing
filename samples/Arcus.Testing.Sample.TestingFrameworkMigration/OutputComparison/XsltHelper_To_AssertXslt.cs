using Codit.Testing.Xslt;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Sample.TestingFrameworkMigration.OutputComparison
{
    public class XsltTests : IntegrationTest
    {
        public XsltTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        /// <summary>
        /// Example using the old Testing Framework to test XSLT transformations.
        /// </summary>
        /// <remarks>
        ///     DO NOT rename the test name, as it is used to retrieve related resources files with the same name.
        /// </remarks>
        [Fact]
        public void TestXslt_XmlToXml_Pass()
        {
            // Arrange
            var fileName = "TestXslt_XmlToXml.xslt";
            var nodesToIgnore = new[] { "Node1" };

            // Act
            bool isSuccess = XsltHelper.TestXslt(fileName, out string failureMessage, xsltArgumentList: null, MessageType.Xml, nodesToIgnore);

            // Assert
            Assert.True(isSuccess, failureMessage);
        }

        /// <summary>
        /// Scenario files are more explicitly shown to the tester, making it easier to understand what the test needs and what the assertion entails.
        /// </summary>
        [Fact]
        public void TestXslt_XmlToXml_PassWithArcus()
        {
            // Arrange
            string xslt = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToXml.xslt");
            string input = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToXml_Pass_input.xml");

            // Act
            string actual = AssertXslt.TransformToXml(xslt, input);

            // Assert
            string expected = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToXml_Pass_expected.xml");
            AssertXml.Equal(expected, actual, opt =>
            {
                opt.IgnoreNode("Node1");
            });
        }

        /// <summary>
        /// Example using the old Testing Framework to test XSLT transformations.
        /// </summary>
        /// <remarks>
        ///     DO NOT rename the test name, as it is used to retrieve related resources files with the same name.
        /// </remarks>
        [Fact]
        public void TestXslt_XmlToJson_Pass()
        {
            // Arrange
            var fileName = "TestXslt_XmlToJson.xslt";

            // Act
            bool isSuccess = XsltHelper.TestXslt(fileName, out string failureMessage, xsltArgumentList: null, MessageType.Json);

            // Assert
            Assert.True(isSuccess, failureMessage);
        }

        /// <summary>
        /// Scenario files are more explicitly shown to the tester, making it easier to understand what the test needs and what the assertion entails.
        /// </summary>
        [Fact]
        public void TestXslt_XmlToJson_PassWithArcus()
        {
            // Arrange
            string xslt = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToJson.xslt");
            string input = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToJson_Pass_input.xml");

            // Act
            string actual = AssertXslt.TransformToJson(xslt, input);

            // Assert
            string expected = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToJson_Pass_expected.json");
            AssertJson.Equal(expected, actual);
        }

        /// <summary>
        /// Example using the old Testing Framework to test XSLT transformations.
        /// </summary>
        /// <remarks>
        ///     DO NOT rename the test name, as it is used to retrieve related resources files with the same name.
        /// </remarks>
        [Fact]
        public void TestXslt_XmlToCsv_Pass()
        {
            // Arrange
            var fileName = "TestXslt_XmlToCsv.xslt";

            // Act
            bool isSuccess = XsltHelper.TestXslt(fileName, out string failureMessage, xsltArgumentList: null, MessageType.Csv);

            // Assert
            Assert.True(isSuccess, failureMessage);
        }

        /// <summary>
        /// Scenario files are more explicitly shown to the tester, making it easier to understand what the test needs and what the assertion entails.
        /// </summary>
        [Fact]
        public void TestXslt_XmlToCsv_PassWithArcus()
        {
            // Arrange
            string xlst = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToCsv.xslt");
            string input = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToCsv_Pass_input.xml");

            // Act
            string actual = AssertXslt.TransformToCsv(xlst, input);

            // Assert
            string expected = ScenarioFiles.ReadFileTextByName("TestXslt_XmlToCsv_Pass_expected.csv");
            AssertCsv.Equal(expected, actual);
        }
    }
}
