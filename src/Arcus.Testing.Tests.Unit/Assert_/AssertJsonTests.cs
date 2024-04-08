using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bogus;
using FsCheck.Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertJsonTests
    {
        private static readonly Faker Bogus = new();

        [Property]
        public void Compare_SameJson_Succeeds()
        {
            string expected = RandomJson();
            string actual = expected;

            AssertJson.Equal(expected, actual);
        }

        [Property]
        public void Compare_WithIgnoreDiff_StillSucceeds()
        {
            // Arrange
            string[] diffExpectedNames = CreateNodeNames("diff-");
            string expected = RandomJson();
            string expectedDiff = diffExpectedNames.Aggregate(expected, AppendNode);

            string[] diffActualNames = CreateNodeNames("diff-");
            string actual = diffActualNames.Aggregate(expected, AppendNode);

            // Act / Assert
            Equal(expectedDiff, actual, options =>
            {
                Assert.All(diffExpectedNames.Concat(diffActualNames), name => options.IgnoreNode(name));
            });
        }

        private static string[] CreateNodeNames(string prefix)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => prefix + CreateNodeName()).ToArray();
        }

        private static string AppendNode(string json, string nodeName)
        {
            JToken token = JToken.Parse(json);
            token[nodeName] = JToken.Parse(CreateNodeValue());

            return token.ToString();
        }

        [Property]
        public void Compare_DiffJson_Fails()
        {
            string expected = RandomJson();
            string actual = RandomJson();

            Assert.Throws<EqualAssertionException>(() => Equal(expected, actual));
        }

        private static string RandomJson()
        {
            int maxDepth = Bogus.Random.Int(1, 3);
            StringBuilder Recurse(StringBuilder acc, int depth)
            {
                if (depth >= maxDepth)
                {
                    string nodeName = CreateNodeName();
                    string nodeValue = CreateNodeValue();
                    acc.AppendLine($"\"{nodeName}\": {nodeValue}");

                    return acc;
                }

                string[] nodeNames = Bogus.Make(Bogus.Random.Int(1, 10), CreateNodeName).ToArray();
                for (var index = 0; index < nodeNames.Length; index++)
                {
                    string name = nodeNames[index];
                    if (Bogus.Random.Bool())
                    {
                        string nodeValue = CreateNodeValue();
                        acc.AppendLine($"\"{name}\": {nodeValue}");
                    }
                    else
                    {
                        string nodeName = CreateNodeName();
                        acc.AppendLine($"\"{nodeName}\": {{");
                        Recurse(acc, depth + 1);
                        acc.AppendLine("}");
                    }

                    bool isNotLast = index < nodeNames.Length - 1;
                    if (isNotLast)
                    {
                        acc.Append(',');
                    }
                }

                return acc;
            }

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder = Recurse(builder, 0);
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string CreateNodeValue()
        {
            return Bogus.PickRandom(
                Bogus.Random.Int().ToString(),
                "\"" + Bogus.Lorem.Word() + "\"",
                Bogus.Random.Bool().ToString().ToLower());
        }

        private static string CreateNodeName()
        {
            return Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..10];
        }

        public static IEnumerable<object[]> FailingBeEquivalentCases
        {
            get
            {
                yield return new object[] { "null", "{ \"id\": 2 }", "is null" };
                yield return new object[] { "{ \"id\": 1 }", "null", "is null" };
                yield return new object[]
                {
                    "{ \"items\": [] }",
                    "{ \"items\": 2 }",
                    "has an array: [] instead of a number: 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "has 2 elements instead of 3 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "{ \"items\": [ \"fork\", \"spoon\", \"knife\" ] }",
                    "has a different value at $.items[1]"
                };
                yield return new object[]
                {
                    "{ \"tree\": { } }",
                    "{ \"tree\": \"oak\" }",
                    "has an object: {} instead of a string: oak at $.tree"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 10} }",
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "{ \"tree\": { \"leaves\": 10} }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 5 } }",
                    "{ \"tree\": { \"leaves\": 10} }",
                    "has a different value at $.tree.leaves"
                };
                yield return new object[]
                {
                    "{ \"eyes\": \"blue\" }",
                    "{ \"eyes\": [] }",
                    "has a string: blue instead of an array: [] at $.eyes"
                };
                yield return new object[]
                {
                    "{ \"eyes\": \"blue\" }",
                    "{ \"eyes\": 2 }",
                    "has a string: blue instead of a number: 2 at $.eyes"
                };
                yield return new object[]
                {
                    "{ \"id\": 1 }",
                    "{ \"id\": 2 }",
                    "has a different value at $.id"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_WithNotEqual_ShouldFailWithDifference(string actualJson, string expectedJson, string expectedDifference)
        {
            CompareShouldFailWithDifference(actualJson, expectedJson, expectedDifference);
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
                Equal(testCase.Item2.ToString(), testCase.Item2.ToString());
            });
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
                    "\"null\"",
                    "{ \"id\": 2 }",
                    "has a string: null instead of an object: {\r\n  \"id\": 2\r\n} at $"
                };
                yield return new object[]
                {
                    "{ \"id\": 1 }",
                    "\"null\"",
                    "has an object: {\r\n  \"id\": 1\r\n} instead of a string: null at $"
                };
                yield return new object[]
                {
                    "{ \"foo\": \"foo\", \"bar\": \"bar\" }",
                    "{ \"baz\": \"baz\" }",
                    "misses property at $.baz"
                };
                yield return new object[]
                {
                    "{ \"items\": [] }",
                    "{ \"items\": 2 }",
                    "has an array: [] instead of a number: 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" ] }",
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "has 2 elements instead of 3 at $.items"
                };
                yield return new object[]
                {
                    "{ \"items\": [ \"fork\", \"knife\" , \"spoon\" ] }",
                    "{ \"items\": [ \"fork\", \"fork\" ] }",
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"tree\": { } }",
                    "{ \"tree\": \"oak\" }",
                    "has an object: {} instead of a string: oak at $.tree"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 10} }",
                    "{ \"tree\": { \"branches\": 5, \"leaves\": 10 } }",
                    "misses property at $.tree.branches"
                };
                yield return new object[]
                {
                    "{ \"tree\": { \"leaves\": 5 } }",
                    "{ \"tree\": { \"leaves\": 10} }",
                    "has a different value at $.tree.leaves"
                };
                yield return new object[]
                {
                    "{ \"eyes\": \"blue\" }",
                    "{ \"eyes\": [] }",
                    "has a string: blue instead of an array: [] at $.eyes"
                };
                yield return new object[]
                {
                    "{ \"eyes\": \"blue\" }",
                    "{ \"eyes\": 2 }",
                    "has a string: blue instead of a number: 2 at $.eyes"
                };
                yield return new object[]
                {
                    "{ \"id\": 1 }",
                    "{ \"id\": 2 }",
                    "has a different value at $.id"
                };
                yield return new object[]
                {
                    "{ \"items\": [ { \"id\": 1 }, { \"id\": 3 }, { \"id\": 5 } ] }",
                    "{ \"items\": [ { \"id\": 1 }, { \"id\": 2 } ] }",
                    "has 3 elements instead of 2 at $.items\r\n"
                };
                yield return new object[]
                {
                    "{ \"foo\": \"1\" }",
                    "{ \"foo\": 1 }",
                    "has a string: 1 instead of a number: 1 at $.foo"
                };
                yield return new object[]
                {
                    "{ \"foo\": \"foo\", \"bar\": \"bar\", \"child\": { \"x\": 1, \"y\": 2, \"grandchild\": { \"tag\": \"abrakadabra\" } } }",
                    "{ \"child\": { \"grandchild\": { \"tag\": \"ooops\" } } }",
                    "misses property at $.foo"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingContainSubtreeCases))]
        public void Compare_WithoutAllNodesFromSubTree_Fails(string actualJson, string expectedJson, string expectedDifference)
        {
            CompareShouldFailWithDifference(actualJson, expectedJson, expectedDifference);
        }

        private static void CompareShouldFailWithDifference(
            string actualJson,
            string expectedJson,
            string expectedDifference)
        {
            var exception = Assert.ThrowsAny<AssertionException>(
                () => Equal(expectedJson, actualJson));

            Assert.Contains(expectedDifference, exception.Message);
        }

        [Fact]
        public void Compare_WithNull_Succeeds()
        {
            Equal("null", "null");
            Equal("{ \"id\": null }", "{ \"Id\": null }");
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
                Equal(testCase.Value, testCase.Key);
            });
        }

        private static void Equal(string expected, string actual, Action<AssertJsonOptions> configureOptions = null)
        {
            if (Bogus.Random.Bool())
            {
                AssertJson.Equal(expected, actual, configureOptions);
            }
            else
            {
                AssertJson.Equal(AssertJson.Load(expected), AssertJson.Load(actual), configureOptions);
            }
        }

        [Fact]
        public void Load_WithInvalidJson_FailsWithDescription()
        {
            var exception = Assert.Throws<JsonException>(() => AssertJson.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertJson), exception.Message);
            Assert.Contains("JSON contents", exception.Message);
        }
    }
}
