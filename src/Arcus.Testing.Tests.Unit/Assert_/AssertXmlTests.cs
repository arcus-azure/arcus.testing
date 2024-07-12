using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertXmlTests
    {
        private readonly ITestOutputHelper _outputWriter;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertXmlTests" /> class.
        /// </summary>
        public AssertXmlTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Property]
        public void CompareWithDefaultIgnoredOrderOption_WithDifferentOrderInput_Succeeds()
        {
            // Arrange
            TestXml expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            expected.Shuffle();

            // Act
            EqualXml(expected, actual);
        }

        [Property]
        public void CompareWithInclude_WithDifferentOrder_FailsWithDescription()
        {
            // Arrange
            TestXml expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            expected.Shuffle();

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, opt => opt.Order = AssertXmlOrder.Include);
        }

        [Property]
        public void Compare_WithoutAttribute_FailsWithDescription()
        {
            // Arrange
            TestXml expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newName = Bogus.Lorem.Word();
            actual.ChangeAttributeName(newName);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "misses an attribute", newName);
        }

        [Property]
        public void Compare_DiffElementName_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newName = TestXml.GenNodeName() + Guid.NewGuid().ToString("N");
            actual.ChangeElementName(newName);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "different name", "element", newName);
        }

        [Fact]
        public void Compare_DiffElementType_FailsWithDescription()
        {
            // Arrange
            var expected = $"<root>{Bogus.Lorem.Word()}</root>";
            var actual = $"<root>{Bogus.Random.Int()}</root>";

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "has a different value at", "while actual");
        }

        [Property]
        public void Compare_DiffElementValue_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newValue = Bogus.Random.AlphaNumeric(Bogus.Random.Int(10, 20));
            actual.ChangeRandomlyElementValue(newValue);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "different value", newValue);
        }

        [Fact]
        public void Compare_WithDiffElementNamespace_FailsWithDescription()
        {
            // Arrange
            string expected = $"<ns:root xmlns:ns=\"{Bogus.Internet.Url()}\"/>";
            string actual = $"<ns:root xmlns:ns=\"{Bogus.Internet.Url()}\"/>";

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "different namespace");
        }

        [Property]
        public void Compare_DiffElementCount_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            actual.InsertElement(TestXml.GenNodeName());

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "has", "element(s)", "instead of");
        }

        [Property]
        public void Compare_WithDiffAttributeName_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newName = TestXml.GenNodeName();
            actual.ChangeAttributeName("diff" + newName);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, options => options.Order = AssertXmlOrder.Include,
                "different name", "attribute", newName);
        }

        [Property]
        public void Compare_WithDiffAttributeValue_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newValue = Bogus.Random.Word();
            actual.ChangeAttributeValue("diff" + newValue);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "different value", "@", newValue);
        }

        [Property]
        public void Compare_WithDiffAttributeCount_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();
            
            actual.InsertAttribute(TestXml.GenNodeName() + Guid.NewGuid().ToString("N"));
            
            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "has", "attribute(s)", "instead of");
        }

        [Property]
        public void Compare_WithDiffAttributeNamespace_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            actual.ChangeAttributeNamespace(Bogus.Internet.Url());

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, options => options.Order = AssertXmlOrder.Include,
                "different namespace", "@");
        }

        public static IEnumerable<object[]> SucceedingBeEquivalentCases
        {
            get
            {
                yield return new object[]
                {
                    $"<root><value>10</value>{Whitespace()}<value>20</value></root>",
                    "<root><value>10</value><value>20</value></root>"
                };
                yield return new object[]
                {
                    $"<value>blue</value{Spaces()}>",
                    $"<value{Spaces()}>blue</value>"
                };
                yield return new object[]
                {
                    "<ns:value xmlns:ns=\"https://same-namespace-different-prefix\"/>",
                    "<nsx:value xmlns:nsx=\"https://same-namespace-different-prefix\"/>",
                };
                yield return new object[]
                {
                    "<value>1012</value>",
                    $"<value><!--{Whitespace()}some additional comment{Whitespace()}-->10<!-- some other comment -->12</value>",
                };
                yield return new object[]
                {
                    "<?xml version=\"1.0\" encoding=\"UTF-16\"?><root/>",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><root/>"
                };
                yield return new object[]
                {
                    "<id></id>",
                    "<id><![CDATA[\r\n   characters with markup]]></id>"
                };
            }
        }

        [Theory]
        [MemberData(nameof(SucceedingBeEquivalentCases))]
        public void Compare_WithEqual_SucceedsNonetheless(string expected, string actual)
        {
            EqualXml(expected, actual);
        }

        private static string Spaces() => string.Concat(Bogus.Make(Bogus.Random.Int(1, 10), () => " "));

        private static string Whitespace()
        {
            return string.Concat(Bogus.Make(Bogus.Random.Int(5, 10), () => Bogus.PickRandom(" ", "\n", "\t", "\r" )));
        }

        public static IEnumerable<object[]> FailingBeEquivalentCases
        {
            get
            {
                yield return new object[]
                {
                    "<items><fork/><knife/></items>",
                    "<items><fork/><knife/><spoon/></items>",
                    "has 3 element(s) instead of 2 at /items"
                };
                yield return new object[]
                {
                    "<items><fork/><knife/><spoon/></items>",
                    "<items><fork/><knife/></items>",
                    "2 element(s) instead of 3 at /items/"
                };
                yield return new object[]
                {
                    "<items><fork/><spoon/><knife/></items>",
                    "<items><fork/><knife/><spoon/></items>",
                    "has a different name at /items/spoon, expected an element: <spoon> while actual an element: <knife>",
                    AssertXmlOrder.Include
                };
                yield return new object[]
                {
                    "<items>2</items>",
                    "<items><branch/></items>",
                    "has a different value at /items/text(), expected a number: 2 while actual an element: <branch>"
                };
                yield return new object[]
                {
                    "<tree>\"oak\"</tree>",
                    "<tree><branch/></tree>",
                    "as a different value at /tree/text(), expected a string: \"oak\" while actual an element: <branch>"
                };
                yield return new object[]
                {
                    "<tree><branches>5</branches><leaves>10</leaves></tree>",
                    "<tree><leaves>10</leaves></tree>",
                    "has 1 element(s) instead of 2 at /tree/"
                };
                yield return new object[]
                {
                    "<tree><leaves>10</leaves></tree>",
                    "<tree><branches>5</branches><leaves>10</leaves></tree>",
                    "has 2 element(s) instead of 1 at /tree/"
                };
                yield return new object[]
                {
                    "<tree><leaves>10</leaves></tree>",
                    "<tree><leaves>5</leaves></tree>",
                    "has a different value at /tree/leaves/text(), expected a number: 10 while actual a number: 5"
                };
                yield return new object[]
                {
                    "<eyes>2</eyes>",
                    "<eyes>\"blue\"</eyes>",
                    "has a different value at /eyes/text(), expected a number: 2 while actual a string: \"blue\""
                };
                yield return new object[]
                {
                    "<id>2</id>",
                    "<id>1</id>",
                    "has a different value at /id"
                };
                yield return new object[]
                {
                    "<tree branches=\"11\" />",
                    "<tree branches=\"10\" />",
                    "has a different value at /tree[@branches], expected a number: 11 while actual a number: 10"
                };
                yield return new object[]
                {
                    "<items meta=\"data\" info=\"root\" />",
                    "<items meta=\"data\" other=\"19\" />",
                    "has a different name at /items[@info], expected an attribute: info while actual an attribute: other",
                    AssertXmlOrder.Include
                };
                yield return new object[]
                {
                    "<items meta=\"data\" info=\"root\" />",
                    "<items meta=\"data\" other=\"19\" />",
                    "misses an attribute: info at /items[@info]",
                    AssertXmlOrder.Ignore
                };
                yield return new object[]
                {
                    "<tree><branches branch_1=\"\" branch_2=\"\"/></tree>",
                    "<tree><branches branch_1=\"\" branch_2=\"\" branch_3=\"\"/></tree>",
                    "3 attribute(s) instead of 2 at /tree/branches"
                };
                yield return new object[]
                {
                    "<tree xmlns:ns1=\"https://nature.com\"><ns1:branch/><ns1:branch/></tree>",
                    "<tree xmlns:ns1=\"https://nature.com\"><ns1:branch/><ns2:branch xmlns:ns2=\"https://diff.com\"/></tree>",
                    "has a different namespace at /tree/branch[1], expected https://nature.com while actual https://diff.com"
                };
                yield return new object[]
                {
                    "<tree><branch xmlns:ns1=\"https://climate.com\" ns1:leaf_1=\"\" ns1:leaf_2=\"\"/></tree>",
                    "<tree><branch xmlns:ns1=\"https://climate.com\" xmlns:ns2=\"https://earth.com\" ns1:leaf_1=\"\" ns2:leaf_2=\"\"/></tree>",
                    "has a different namespace at /tree/branch[@leaf_2], expected https://climate.com while actual https://earth.com",
                    AssertXmlOrder.Include
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_WithNotEqual_ShouldFailWithDifference(string expectedXml, string actualXml, string expectedDifference, AssertXmlOrder? order = null)
        {
            CompareShouldFailWithDifference(expectedXml, actualXml, options => options.Order = order ?? options.Order, expectedDifference);
        }

        [Property]
        public void Compare_SameXml_Succeeds()
        {
            string expected = TestXml.Generate().ToString();
            string actual = expected;

            EqualXml(expected, actual);
        }

        [Property]
        public void Compare_WithIgnoreDiff_StillSucceeds()
        {
            // Arrange
            var expected = TestXml.Generate();
            string[] diffExpectedNames = CreateNodeNames("diff");
            InsertDiffNodes(expected, diffExpectedNames);

            TestXml actual = expected.Copy();

            string[] diffActualNames = CreateNodeNames("diff-");
            InsertDiffNodes(actual, diffActualNames);

            // Act / Assert
            EqualXml(expected, actual, 
                options => Assert.All(diffActualNames.Concat(diffExpectedNames), name =>
                {
                    options.IgnoreNode(name);
                }));
        }

        private static void InsertDiffNodes(TestXml xml, string[] names)
        {
            Assert.All(names, name =>
            {
                if (Bogus.Random.Bool())
                {
                    xml.InsertElement(name);
                }
                else
                {
                    xml.InsertAttribute(name);
                }
            });
        }

        [Fact]
        public void Compare_WithIgnoreRoot_FailsWithInvalid()
        {
            // Arrange
            XmlNode expected = AssertXml.Load(TestXml.Generate().ToString());
            XmlNode actual = AssertXml.Load(TestXml.Generate().ToString());

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(
                () => EqualXml(expected.OuterXml, actual.OuterXml, options => options.IgnoreNode(expected.LocalName)));
        }

        private static string[] CreateNodeNames(string prefix)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => prefix + TestXml.GenNodeName()).ToArray();
        }

        [Property]
        public void Compare_DiffXml_Fails()
        {
            string expected = TestXml.Generate().ToString();
            string actual = TestXml.Generate().ToString();

            Assert.ThrowsAny<AssertionException>(() => EqualXml(expected, actual));
        }

        private void CompareShouldFailWithDifference(TestXml expected, TestXml actual, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expected, actual, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(TestXml expected, TestXml actual, Action<AssertXmlOptions> configureOptions, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expected.ToString(), actual.ToString(), configureOptions, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(string expectedXml, string actualXml, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expectedXml, actualXml, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(string expectedXml, string actualXml, Action<AssertXmlOptions> configureOptions, params string[] expectedDifferences)
        {
            try
            {
                var exception = Assert.ThrowsAny<AssertionException>(() => EqualXml(expectedXml, actualXml, configureOptions));
                Assert.Contains(nameof(AssertXml), exception.Message);
                Assert.Contains("XML documents", exception.Message);
                Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
            }
            catch (XunitException)
            {
                _outputWriter.WriteLine("{0}: {1}", Environment.NewLine + "Expected", expectedXml + Environment.NewLine);
                _outputWriter.WriteLine("{0}: {1}", Environment.NewLine + "Actual", actualXml + Environment.NewLine);
                throw;
            }
        }

        private void EqualXml(TestXml expected, TestXml actual, Action<AssertXmlOptions> configureOptions = null)
        {
            EqualXml(expected.ToString(), actual.ToString(), configureOptions);
        }

        private void EqualXml(string expected, string actual, Action<AssertXmlOptions> configureOptions = null)
        {
            void ConfigureOptions(AssertXmlOptions options)
            {
                options.MaxInputCharacters = int.MaxValue;
                configureOptions?.Invoke(options);
            }

            if (Bogus.Random.Bool())
            {
                AssertXml.Equal(expected, actual, ConfigureOptions);
            }
            else
            {
                AssertXml.Equal(AssertXml.Load(expected), AssertXml.Load(actual), ConfigureOptions);
            }
        }

        [Fact]
        public void Load_WithInvalidXml_FailsWithDescription()
        {
            var exception = Assert.Throws<XmlException>(() => AssertXml.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertXml), exception.Message);
            Assert.Contains("XML contents", exception.Message);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void IgnoreNode_WithoutValue_Fails(string nodeName)
        {
            // Arrange
            var options = new AssertXmlOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.IgnoreNode(nodeName));
        }

        [Fact]
        public void MaxInputCharacters_WithNegativeValue_Fails()
        {
            // Arrange
            var options = new AssertXmlOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MaxInputCharacters = Bogus.Random.Int(max: -1));
        }

        [Fact]
        public void Order_OutsideEnumeration_Fails()
        {
            // Arrange
            var options = new AssertXmlOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Order = (AssertXmlOrder) Bogus.Random.Int(min: 2));
        }
    }
}
