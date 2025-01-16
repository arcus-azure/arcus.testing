using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck;
using FsCheck.Xunit;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static System.Environment;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertJsonTests
    {
        private readonly ResourceDirectory _resourceDir;
        private readonly ITestOutputHelper _outputWriter;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertJsonTests" /> class.
        /// </summary>
        public AssertJsonTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _resourceDir = ResourceDirectory.CurrentDirectory.WithSubDirectory(nameof(Assert_)).WithSubDirectory("Resources");
        }

        [Property]
        public void CompareWithDefaultIgnoredOrderOption_WithDifferentOrderInput_Succeeds()
        {
            // Arrange
            TestJson expected = TestJson.Generate();
            TestJson actual = expected.Copy();

            expected.Shuffle();

            // Act
            EqualJson(expected, actual);
        }

        [Property]
        public Property CompareWithIncludeOrderOption_WithDifferentOrder_FailsWithDescription()
        {
            // Arrange
            TestJson expected = TestJson.GenerateArray();
            TestJson actual = expected.Copy();

            expected.Shuffle();

            // Act / Assert
            return Prop.When(expected != actual, () => CompareShouldFailWithDifference(actual, expected, options => options.Order = AssertJsonOrder.Include));
        }

        [Property]
        public void Compare_WithDifferentPropertyName_FailsWithDescription()
        {
            // Arrange
            TestJson expected = TestJson.GenerateObject();
            TestJson actual = expected.Copy();

            string newName = Bogus.Lorem.Word();
            actual.InsertProperty(newName);

            // Act / Assert
            CompareShouldFailWithDifference(expected, actual, "misses property", newName);
        }

        [Property]
        public void Compare_SameJson_Succeeds()
        {
            TestJson expected = TestJson.Generate();
            TestJson actual = expected.Copy();

            AssertJson.Equal(expected.ToString(), actual.ToString());
        }

        [Property]
        public void Compare_WithIgnoreDiff_StillSucceeds()
        {
            // Arrange
            string[] diffExpectedNames = CreateNodeNames("diff-");
            TestJson expected = TestJson.GenerateObject();
            TestJson actual = expected.Copy();

            Assert.All(diffExpectedNames, expected.InsertProperty);

            string[] diffActualNames = CreateNodeNames("diff-");
            Assert.All(diffActualNames, actual.InsertProperty);

            // Act / Assert
            EqualJson(expected.ToString(), actual.ToString(), options =>
            {
                Assert.All(diffExpectedNames.Concat(diffActualNames), name => options.IgnoreNode(name));
            });
        }

        private static string[] CreateNodeNames(string prefix)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => prefix + CreateNodeName()).ToArray();
        }

        [Property]
        public void Compare_DiffJson_Fails()
        {
            TestJson expected = TestJson.Generate();
            TestJson actual = TestJson.Generate();

            Assert.Throws<EqualAssertionException>(() => EqualJson(expected.ToString(), actual.ToString()));
        }

        private static string CreateNodeName()
        {
            return Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..10];
        }

        public static IEnumerable<object[]> FailingBeEquivalentCases
        {
            get
            {
                yield return new object[] { "{ \"id\": 2 }", "null", "is null" };
                yield return new object[] { "null", "{ \"id\": 1 }", "is null" };
                yield return new object[]
                {
                    "{ \"items\": 2 }",
                    "{ \"items\": [] }",
                    "different type at $.items, expected a number: 2 while actual an array: []"
                };
                yield return new object[]
                {
                    "{ \"items\": 2 }",
                    "{ \"items\": null }",
                    "actual JSON is null at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": null }",
                    "{ \"items\": [] }",
                    "actual JSON has a different type at $.items, expected type null while actual an array: []"
                };
                yield return new object[]
                {
                    "{ \"items\": [null] }",
                    "{ \"items\": [] }",
                    "actual JSON has 0 elements instead of 1 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [] }",
                    "{ \"items\": [null] }",
                    "actual JSON has 1 elements instead of 0 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "has 2 elements instead of 3 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"spoons\", \"knife\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "has a different value at $.items[1]"
                };
                yield return new object[]
                {
                    "{ \"tree\": \"oak\" }",
                    "{ \"tree\": { } }",
                    "different type at $.tree, expected a string: oak while actual an object: {}"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"name\": \"oak\" } }",
                    "{ \"tree\": null }",
                    "actual JSON is null at $.tree"
                };
                yield return new object[]
                {
                    "{ \"tree\": null }",
                    "{ \"tree\": { \"name\": \"oak\" } }",
                    "actual JSON has a different type at $.tree, expected type null while actual an object: {\"name\":\"oak\"}"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"name\": null } }",
                    "{ \"tree\": { \"name\": \"oak\" } }",
                    "actual JSON has a different type at $.tree.name, expected type null while actual a string: oak"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "{ \"tree\": { \"leaves\": 10} }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 10} }",
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 10} }",
                    "{ \"tree\": { \"leaves\": 5 } }",
                    "different value at $.tree.leaves, expected a number: 10 while actual a number: 5"
                };
                yield return new object[]
                {
                    "{ \"eyes\": [] }",
                    "{ \"eyes\": \"blue\" }",
                    "different type at $.eyes, expected an array: [] while actual a string: blue"
                };
                yield return new object[]
                {
                    "{ \"eyes\": 2 }",
                    "{ \"eyes\": \"blue\" }",
                    "at $.eyes, expected a number: 2 while actual a string: blue"
                };
                yield return new object[]
                {
                    "{ \"id\": 2 }",
                    "{ \"id\": 1 }",
                    "different value at $.id, expected a number: 2 while actual a number: 1"
                };
                yield return new object[]
                {
                    "[ \"horse\", \"dog\" ]",
                    "[ \"dog\", \"horse\" ]",
                    "has a different value at $[0]",
                    AssertJsonOrder.Include
                };
                yield return new object[]
                {
                    "{ \"leaves\": [ 1, 2, 3 ] }",
                    "{ \"leaves\": [ 2, 3, 1 ] }",
                    "has a different value at $.leaves[0]",
                    AssertJsonOrder.Include
                };
                yield return new object[]
                {
                    "{\"Products\":[{\"id\":3},{\"id\":3},{\"id\":1}]}",
                    "{\"Products\":[{\"id\":1},{\"id\":2},{\"id\":3}]}",
                    "has a different value at $.Products[0].id",
                    AssertJsonOrder.Ignore
                };
                yield return new object[]
                {
                    "{\"Products\":[{\"id\":3},{\"id\":3},{\"id\":1}]}",
                    "{\"Products\":[1,2,3]}",
                    "has a different type at $.Products",
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_WithNotEqual_ShouldFailWithDifference(string expectedJson, string actualJson, string expectedDifference, AssertJsonOrder? order = null)
        {
            CompareShouldFailWithDifference(expectedJson, actualJson, options => options.Order = order ?? options.Order, expectedDifference);
        }

        [Fact]
        public void Compare_ArraysWithSamePropertiesInProperOrder_Succeeds()
        {
            var testCases = new[]
            {
                Tuple.Create(
                    new JArray(1, 2, 3),
                    new JArray(1, 2, 3))
                ,
                Tuple.Create(
                    new JArray("blue", "green"),
                    new JArray("blue", "green"))
                ,
                Tuple.Create(
                    new JArray(JToken.Parse("{ car: { color: \"blue\" }}"), JToken.Parse("{ flower: { color: \"red\" }}")),
                    new JArray(JToken.Parse("{ car: { color: \"blue\" }}"), JToken.Parse("{ flower: { color: \"red\" }}")))
            };

            Assert.All(testCases, testCase =>
            {
                EqualJson(testCase.Item2.ToString(), testCase.Item2.ToString());
            });
        }

        [Theory]
        [InlineData("[ 1, 2, 3]", "[ 2, 1, 3 ]")]
        [InlineData("[ \"2\", \"3\", \"1\" ]", "[ \"1\", \"2\", \"3\" ]")]
        [InlineData("[ true, false, true ]", "[ false, true, true ]")]
        [InlineData("{\"Products\":[{\"id\":1},{\"id\":2},{\"id\":3}]}", "{\"Products\":[{\"id\":3},{\"id\":2},{\"id\":1}]}")]
        [InlineData("{\"Products\":[{\"id\":[1]},{\"id\":[2]},{\"id\":[3]}]}", "{\"Products\":[{\"id\":[3]},{\"id\":[2]},{\"id\":[1]}]}")]
        [InlineData("[ { \"id\": 2 }, { \"id\": 1 } ]", "[ { \"id\": 1 }, { \"id\": 2 } ]")]
        [InlineData(
            "[ { \"id\": 1 }, { \"name\": [ { \"name\": \"testing\", \"project\": \"arcus\" } ] } ]",
            "[ { \"name\": [ { \"project\": \"arcus\", \"name\": \"testing\" } ] }, { \"id\": 1 } ]")]
        public void Compare_ArraysWithSameValuesInDifferentOrder_StillSucceeds(string expected, string actual)
        {
            EqualJson(expected, actual);
        }

        [Fact]
        public void BugFixCompare_ArraysWithSameValuesInDifferentOrder_StillSucceeds()
        {
            // Arrange
            string fileNamePrefix = "json.ignored.order.objects.in.array.sample";
            string actual = _resourceDir.ReadFileTextByName(fileNamePrefix + ".actual.json");
            string expected = _resourceDir.ReadFileTextByName(fileNamePrefix + ".expected.json");

            // Act / Assert
            EqualJson(expected, actual);
        }

        [Fact]
        public void Compare_ObjectWithUnorderedProperties_StillSucceeds()
        {
            var testCases = new Dictionary<string, string>
            {
                {
                    "{ \"friends\": [{ \"id\": 123, \"name\": \"Corby Page\" }, { \"id\": 456, \"name\": \"Carter Page\" }] }",
                    "{ \"friends\": [{ \"name\": \"Corby Page\", \"id\": 123 }, { \"id\": 456, \"name\": \"Carter Page\" }] }"
                },
                {
                    "{ \"id\": 2, \"admin\": true }",
                    "{ \"admin\": true, \"id\": 2}"
                }
            };

            Assert.All(testCases, testCase =>
            {
                AssertJson.Equal(testCase.Key, testCase.Value);
            });
        }

        public static IEnumerable<object[]> FailingContainSubtreeCases
        {
            get
            {
                yield return new object[]
                {
                    "{ \"id\": 2 }",
                    "\"null\"",
                    "different type at $, expected an object: {\"id\":2} while actual a string: null"
                };
                yield return new object[]
                {
                    "\"null\"",
                    "{ \"id\": 1 }",
                    "different type at $, expected a string: null while actual an object: {\"id\":1}"
                };
                yield return new object[]
                {
                    "{ \"baz\": \"baz\" }",
                    "{ \"foo\": \"foo\", \"bar\": \"bar\" }",
                    "misses property at $.baz"
                };
                yield return new object[]
                {
                    "{ \"items\": 2 }",
                    "{ \"items\": [] }",
                    "different type at $.items, expected a number: 2 while actual an array: []"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "has 2 elements instead of 3 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"fork\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"tree\": \"oak\" }",
                    "{ \"tree\": { } }",
                    "different type at $.tree, expected a string: oak while actual an object: {}"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "{ \"tree\": { \"leaves\": 10} }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 10} }",
                    "{ \"tree\": { \"leaves\": 5 } }",
                    "different value at $.tree.leaves, expected a number: 10 while actual a number: 5"
                };
                yield return new object[]
                {
                    "{ \"eyes\": [] }",
                    "{ \"eyes\": \"blue\" }",
                    "different type at $.eyes, expected an array: [] while actual a string: blue"
                };
                yield return new object[]
                {
                    "{ \"eyes\": 2 }",
                    "{ \"eyes\": \"blue\" }",
                    "at $.eyes, expected a number: 2 while actual a string: blue"
                };
                yield return new object[]
                {
                    "{ \"id\": 2 }",
                    "{ \"id\": 1 }",
                    "different value at $.id, expected a number: 2 while actual a number: 1"
                };
                yield return new object[]
                {
                    "{ \"items\": [ { \"id\": 1 }, { \"id\": 2 } ] }",
                    "{ \"items\": [ { \"id\": 1 }, { \"id\": 3 }, { \"id\": 5 } ] }",
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"foo\": 1 }",
                    "{ \"foo\": \"test\" }",
                    "at $.foo, expected a number: 1 while actual a string: test"
                };
                yield return new object[]
                {
                    "{ \"child\": { \"grandchild\": { \"tag\": \"ooops\" } }, \"bar\": \"bar\" }",
                    "{ \"foo\": \"foo\", \"bar\": \"bar\", \"child\": { \"x\": 1, \"y\": 2, \"grandchild\": { \"tag\": \"abrakadabra\" } } }",
                    "misses property at $.foo"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingContainSubtreeCases))]
        public void Compare_WithoutAllNodesFromSubTree_Fails(string expectedJson, string actualJson, string expectedDifference)
        {
            CompareShouldFailWithDifference(expectedJson, actualJson, expectedDifference);
        }

        private void CompareShouldFailWithDifference(TestJson expected, TestJson actual, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expected, actual, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(TestJson expected, TestJson actual, Action<AssertJsonOptions> configureOptions, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expected.ToString(), actual.ToString(), configureOptions, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(string expectedJson, string actualJson, params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(expectedJson, actualJson, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDifference(string expectedJson, string actualJson, Action<AssertJsonOptions> configureOptions, params string[] expectedDifferences)
        {
            try
            {
                var exception = Assert.ThrowsAny<AssertionException>(() => EqualJson(expectedJson, actualJson, configureOptions));
                Assert.Contains(nameof(AssertJson), exception.Message);
                Assert.Contains("JSON contents", exception.Message);
                Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
            }
            catch (XunitException)
            {
                _outputWriter.WriteLine("{0}: {1}", NewLine + "Expected", expectedJson + NewLine);
                _outputWriter.WriteLine("{0}: {1}", NewLine + "Actual", actualJson + NewLine);
                throw;
            }
        }

        [Fact]
        public void Compare_WithNull_Succeeds()
        {
            EqualJson("null", "null");
            EqualJson("{ \"id\": null }", "{ \"Id\": null }");
        }

        [Fact]
        public void Compare_WithProperties_Succeeds()
        {
            // Arrange
            var testCases = new Dictionary<string, string>
            {
                {
                    "{ \"friends\": [{ \"id\": 123, \"name\": \"Corby Page\" }, { \"id\": 456, \"name\": \"Carter Page\" }] }",
                    "{ \"friends\": [{ \"name\": \"Corby Page\", \"id\": 123 }, { \"id\": 456, \"name\": \"Carter Page\" }] }"
                },
                {
                    "{ \"id\": 2, \"admin\": true }",
                    "{ \"admin\": true, \"id\": 2}"
                }
            };

            Assert.All(testCases, testCase =>
            {
                EqualJson(testCase.Value, testCase.Key);
            });
        }

        //"► ◄"
        public static IEnumerable<object[]> FailingCasesWithScopedExpectedActualDifferences
        {
            get
            {
                yield return new object[]
                {
                    "{ \"movies\": [ \"The Matrix\", \"Blade Runner\", \"Terminator\" ] }",
                    "{ \"movies\": [ \"The Matrix\", \"Blade Runner\" ] }",
@"Expected:            Actual:
[                    [
  ""The Matrix"",        ""The Matrix"",
  ""Blade Runner"",      ""Blade Runner""
  ""Terminator""       ]
]"
                };
                yield return new object[]
                {
                    "[ { \"title\": \"Ubik\", \"author\": \"Philip K. Dick\" } ]",
                    "[ { \"title\": \"Ubik\", \"author\": \"Richard K. Morgan\" } ]",
@"Expected:                       Actual:
{                               {
  ""title"": ""Ubik"",                ""title"": ""Ubik"",
  ""author"": ""Philip K. Dick""      ""author"": ""Richard K. Morgan""
}                               }"
                };
                yield return new object[]
                {
                    "{ \"band\": \"Cult of Luna\", \"genre\": \"post-metal\" }",
                    "{ \"band\": \"Cult of Luna\", \"genre\": { \"name\": \"post-metal\" } }",
@"Expected:     Actual:
post-metal    {
                ""name"": ""post-metal""
              }"
                };
                yield return new object[]
                {
                    "{ \"this\": \"that\", \"options\": null }",
                    "{ \"this\": \"that\", \"options\": { \"and\": \"this\" } }",
@"Expected:    Actual:
null         {
               ""and"": ""this""
             }"
                };
                yield return new object[]
                {
                    "{ \"this\": \"that\", \"options\": { \"and\": \"this\" } }",
                    "{ \"this\": \"that\", \"options\": null }",
@"Expected:          Actual:
{                  null
  ""and"": ""this""    
}"
                };
                yield return new object[]
                {
                    "null",
                    "[ \"this\", \"that\" ]",
@"Expected:    Actual:
null         [
               ""this"",
               ""that""
             ]"
                };
                yield return new object[]
                {
                    "[ \"this\", \"that\" ]",
                    "null",
@"Expected:    Actual:
[            null
  ""this"",    
  ""that""     
] "
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingCasesWithScopedExpectedActualDifferences))]
        public void CompareJson_WithDifferences_ShouldScopeToDifference(
            string expected,
            string actual,
            string expectedDifferences)
        {
            CompareShouldFailWithDifference(expected, actual, expectedDifferences);
        }

        public static IEnumerable<object[]> FailingCasesWithReportOptions
        {
            get
            {
                yield return new object[]
                {
                    "{ \"tree\": [] }",
                    "{ \"branch\": [] }",
                    new Action<AssertJsonOptions>(options => options.ReportFormat = ReportFormat.Vertical),
                    $"Expected:{NewLine}{{{NewLine}  \"tree\": []{NewLine}}}{NewLine}{NewLine}Actual:{NewLine}{{{NewLine}  \"branch\": []{NewLine}}}"
                };
                yield return new object[]
                {
                    "{ \"tree\": [] }",
                    "{ \"branch\": [] }",
                    new Action<AssertJsonOptions>(options => options.ReportFormat = ReportFormat.Horizontal),
                    $"Expected:       Actual:{NewLine}{{               {{{NewLine}  \"tree\": []      \"branch\": []{NewLine}}}               }}"
                };
                yield return new object[]
                {
                    "{ \"tree\": [ { \"branch\": 1 } ] }",
                    "{ \"tree\": [ { \"leaf\": 1 } ] }",
                    new Action<AssertJsonOptions>(options => options.ReportScope = ReportScope.Limited),
                    $"{{                {{{NewLine}  \"branch\": 1      \"leaf\": 1{NewLine}}}                }}"
                };
                yield return new object[]
                {
                    "{ \"tree\": [ { \"branch\": 1 } ] }",
                    "{ \"tree\": [ { \"leaf\": 1 } ] }",
                    new Action<AssertJsonOptions>(options => options.ReportScope = ReportScope.Complete),
                    $"Expected:            Actual:{NewLine}{{                    {{{NewLine}  \"tree\": [            \"tree\": [{NewLine}    {{                    {{{NewLine}      \"branch\": 1          \"leaf\": 1{NewLine}    }}                    }}{NewLine}  ]                    ]{NewLine}}}                    }}"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingCasesWithReportOptions))]
        public void CompareJson_WithReportOptions_ShouldCreateReportByOptions(
            string expected,
            string actual,
            Action<AssertJsonOptions> configureOptions,
            string expectedDifferences)
        {
            CompareShouldFailWithDifference(expected, actual, configureOptions, expectedDifferences);
        }

        private static void EqualJson(TestJson expected, TestJson actual, Action<AssertJsonOptions> configureOptions = null)
        {
            EqualJson(expected.ToString(), actual.ToString(), configureOptions);
        }

        private static void EqualJson(string expected, string actual, Action<AssertJsonOptions> configureOptions = null)
        {
            void ConfigureOptions(AssertJsonOptions options)
            {
                options.MaxInputCharacters = int.MaxValue;
                configureOptions?.Invoke(options);
            }

            if (Bogus.Random.Bool())
            {
                AssertJson.Equal(expected, actual, ConfigureOptions);
            }
            else
            {
                AssertJson.Equal(
                    AssertJson.Load(expected, opt => opt.PropertyNameCaseInsensitive = true, configureDocOptions: null),
                    AssertJson.Load(actual, opt => opt.PropertyNameCaseInsensitive = true, configureDocOptions: null),
                    ConfigureOptions);
            }
        }

        [Fact]
        public void Load_WithInvalidJson_FailsWithDescription()
        {
            var exception = Assert.Throws<System.Text.Json.JsonException>(
                () => AssertJson.Load(Bogus.Random.Utf16String()));

            Assert.Contains(nameof(AssertJson), exception.Message);
            Assert.Contains("JSON contents", exception.Message);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void IgnoreNode_WithoutValue_Fails(string nodeName)
        {
            // Arrange
            var options = new AssertJsonOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.IgnoreNode(nodeName));
        }

        [Fact]
        public void MaxInputCharacters_WithNegativeValue_Fails()
        {
            // Arrange
            var options = new AssertJsonOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MaxInputCharacters = Bogus.Random.Int(max: -1));
        }

        [Fact]
        public void Order_OutsideEnumeration_Fails()
        {
            // Arrange
            var options = new AssertJsonOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Order = (AssertJsonOrder) Bogus.Random.Int(min: 2));
        }
    }
}
