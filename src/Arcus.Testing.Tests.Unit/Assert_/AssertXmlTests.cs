using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertXmlTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void Compare_DiffElementName_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newName = TestXml.GenNodeName();
            actual.ChangeElementName(newName);

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different name", "element", newName);
        }

        [Fact]
        public void Compare_DiffElementType_FailsWithDescription()
        {
            // Arrange
            var expected = $"<root>{Bogus.Random.Word()}</root>";
            var actual = $"<root>{Bogus.Random.Int()}</root>";

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "has a different value at", "while actual");
        }

        [Fact]
        public void Compare_DiffElementValue_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newValue = Bogus.Random.AlphaNumeric(Bogus.Random.Int(1, 10));
            actual.ChangeRandomlyElementValue(newValue);

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different value", newValue);
        }

        [Fact]
        public void Compare_WithDiffElementNamespace_FailsWithDescription()
        {
            // Arrange
            string expected = $"<ns:root xmlns:ns=\"{Bogus.Internet.Url()}\"/>";
            string actual = $"<ns:root xmlns:ns=\"{Bogus.Internet.Url()}\"/>";

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different namespace");
        }

        [Fact]
        public void Compare_DiffElementCount_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            actual.InsertElement(TestXml.GenNodeName());

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "has", "element(s)", "instead of");
        }

        [Fact]
        public void Compare_WithDiffAttributeName_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newName = TestXml.GenNodeName();
            actual.ChangeAttributeName("diff" + newName);

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different name", "attribute", newName);
        }

        [Fact]
        public void Compare_WithDiffAttributeValue_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            string newValue = Bogus.Random.Word();
            actual.ChangeAttributeValue("diff" + newValue);

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different value", "@", newValue);
        }

        [Fact]
        public void Compare_WithDiffAttributeCount_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();
            
            actual.InsertAttribute(TestXml.GenNodeName());
            
            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "has", "attribute(s)", "instead of");
        }

        [Fact]
        public void Compare_WithDiffAttributeNamespace_FailsWithDescription()
        {
            // Arrange
            var expected = TestXml.Generate();
            TestXml actual = expected.Copy();

            actual.ChangeAttributeNamespace(Bogus.Internet.Url());

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "different namespace", "@");
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
            Equal(expected, actual);
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
                    "<items><fork/><knife/><spoon/></items>",
                    "<items><fork/><knife/></items>",
                    "has 3 element(s) instead of 2 at /items"
                };
                yield return new object[]
                {
                    "<items><fork/><knife/></items>",
                    "<items><fork/><knife/><spoon/></items>",
                    "2 element(s) instead of 3 at /items/"
                };
                yield return new object[]
                {
                    "<items><fork/><knife/><spoon/></items>",
                    "<items><fork /><spoon/><knife/></items>",
                    "has a different name at /items/spoon[1], expected an element: <spoon> while actual an element: <knife>"
                };
                yield return new object[]
                {
                    "<items><branch/></items>",
                    "<items>2</items>",
                    "has a different value at /items/text(), expected a number: 2 while actual an element: <branch>"
                };
                yield return new object[]
                {
                    "<tree><branch/></tree>",
                    "<tree>\"oak\"</tree>",
                    "as a different value at /tree/text(), expected a string: \"oak\" while actual an element: <branch>"
                };
                yield return new object[]
                {
                    "<tree><leaves>10</leaves></tree>",
                    "<tree><branches>5</branches><leaves>10</leaves></tree>",
                    "has 1 element(s) instead of 2 at /tree/"
                };
                yield return new object[]
                {
                    "<tree><branches>5</branches><leaves>10</leaves></tree>",
                    "<tree><leaves>10</leaves></tree>",
                    "has 2 element(s) instead of 1 at /tree/"
                };
                yield return new object[]
                {
                    "<tree><leaves>5</leaves></tree>",
                    "<tree><leaves>10</leaves></tree>",
                    "has a different value at /tree/leaves/text(), expected a number: 10 while actual a number: 5"
                };
                yield return new object[]
                {
                    "<eyes>\"blue\"</eyes>",
                    "<eyes>2</eyes>",
                    "has a different value at /eyes/text(), expected a number: 2 while actual a string: \"blue\""
                };
                yield return new object[]
                {
                    "<id>1</id>",
                    "<id>2</id>",
                    "has a different value at /id"
                };
                yield return new object[]
                {
                    "<tree branches=\"10\" />",
                    "<tree branches=\"11\" />",
                    "has a different value at /tree[@branches], expected a number: 11 while actual a number: 10"
                };
                yield return new object[]
                {
                    "<items meta=\"data\" other=\"19\" />",
                    "<items meta=\"data\" info=\"root\" />",
                    "has a different name at /items[@info], expected an attribute: info while actual an attribute: other"
                };
                yield return new object[]
                {
                    "<tree><branches branch_1=\"\" branch_2=\"\" branch_3=\"\"/></tree>",
                    "<tree><branches branch_1=\"\" branch_2=\"\"/></tree>",
                    "3 attribute(s) instead of 2 at /tree/branches"
                };
                yield return new object[]
                {
                    "<tree xmlns:ns1=\"https://nature.com\"><ns1:branch/><ns2:branch xmlns:ns2=\"https://diff.com\"/></tree>",
                    "<tree xmlns:ns1=\"https://nature.com\"><ns1:branch/><ns1:branch/></tree>",
                    "has a different namespace at /tree/branch[1], expected https://nature.com while actual https://diff.com"
                };
                yield return new object[]
                {
                    "<tree><branch xmlns:ns1=\"https://climate.com\" xmlns:ns2=\"https://earth.com\" ns1:leaf_1=\"\" ns2:leaf_2=\"\"/></tree>",
                    "<tree><branch xmlns:ns1=\"https://climate.com\" ns1:leaf_1=\"\" ns1:leaf_2=\"\"/></tree>",
                    "has a different namespace at /tree/branch[@leaf_2], expected https://climate.com while actual https://earth.com"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_WithNotEqual_ShouldFailWithDifference(string actualXml, string expectedXml, string expectedDifference)
        {
            CompareShouldFailWithDifference(actualXml, expectedXml, expectedDifference);
        }

        [Property]
        public void Compare_SameXml_Succeeds()
        {
            string expected = TestXml.Generate().ToString();
            string actual = expected;

            Equal(expected, actual);
        }

        [Property]
        public void Compare_WithIgnoreDiff_StillSucceeds()
        {
            // Arrange
            var expected = TestXml.Generate();
            string[] diffExpectedNames = CreateNodeNames("diff");
            InsertDiffNodes(expected, diffExpectedNames);

            var actual = expected.Copy();

            string[] diffActualNames = CreateNodeNames("diff-");
            InsertDiffNodes(actual, diffActualNames);

            // Act / Assert
            Equal(expected.ToString(), actual.ToString(), 
                options => Assert.All(diffActualNames.Concat(diffExpectedNames), name => options.IgnoreNode(name)));
        }

        private void InsertDiffNodes(TestXml xml, string[] names)
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
                () => Equal(expected.OuterXml, actual.OuterXml, options => options.IgnoreNode(expected.LocalName)));
        }

        private static string[] CreateNodeNames(string prefix)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => prefix + TestXml.GenNodeName()).ToArray();
        }

        [Fact]
        public void Compare_DiffXml_Fails()
        {
            string expected = TestXml.Generate().ToString();
            string actual = TestXml.Generate().ToString();

            Assert.ThrowsAny<AssertionException>(() => Equal(expected, actual));
        }


        private static void CompareShouldFailWithDifference(TestXml actual, TestXml expected, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(actual.ToString(), expected.ToString(), expectedDifferences);
        }

        private static void CompareShouldFailWithDifference(string actualXml, string expectedXml, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => Equal(expectedXml, actualXml, options => options.MaxInputCharacters = int.MaxValue));
            Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
        }

        private static void Equal(string expected, string actual, Action<AssertXmlOptions> configureOptions = null)
        {
            if (Bogus.Random.Bool())
            {
                AssertXml.Equal(expected, actual, configureOptions);
            }
            else
            {
                AssertXml.Equal(AssertXml.Load(expected), AssertXml.Load(actual), configureOptions);
            }
        }

        [Fact]
        public void Load_WithInvalidXml_FailsWithDescription()
        {
            var exception = Assert.Throws<XmlException>(() => AssertXml.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertXml), exception.Message);
            Assert.Contains("XML contents", exception.Message);
        }
    }
}
