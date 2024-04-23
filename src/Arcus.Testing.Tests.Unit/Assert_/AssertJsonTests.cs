﻿using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertJsonTests
    {
        private static readonly Faker Bogus = new();

        [Property]
        public void Compare_WithDifferentPropertyName_FailsWithDescription()
        {
            // Arrange
            TestJson expected = TestJson.GenerateObject();
            TestJson actual = expected.Copy();

            string newName = Bogus.Lorem.Word();
            actual.InsertProperty(newName);

            // Act / Assert
            CompareShouldFailWithDifference(actual, expected, "misses property", newName);
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
            Equal(expected.ToString(), actual.ToString(), options =>
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

            Assert.Throws<EqualAssertionException>(() => Equal(expected.ToString(), actual.ToString()));
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
                    "has a string: null instead of an object: {\"id\":2} at $"
                };
                yield return new object[]
                {
                    "{ \"id\": 1 }",
                    "\"null\"",
                    "has an object: {\"id\":1} instead of a string: null at $"
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
                    "has 3 elements instead of 2 at $.items"
                };
                yield return new object[]
                {
                    "{ \"foo\": \"test\" }",
                    "{ \"foo\": 1 }",
                    "has a string: test instead of a number: 1 at $.foo"
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
            TestJson actual,
            TestJson expected,
            params string[] expectedDifferences)
        {
            CompareShouldFailWithDifference(actual.ToString(), expected.ToString(), expectedDifferences);
        }

        private static void CompareShouldFailWithDifference(
            string actualJson,
            string expectedJson,
            params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(
                () => Equal(expectedJson, actualJson, options => options.MaxInputCharacters = int.MaxValue));

            Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
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
                AssertJson.Equal(
                    AssertJson.Load(expected, opt => opt.PropertyNameCaseInsensitive = true, configureDocOptions: null), 
                    AssertJson.Load(actual, opt => opt.PropertyNameCaseInsensitive = true, configureDocOptions: null), 
                    configureOptions);
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
    }
}