using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Arcus.Testing.Tests.Core.Integration.DataFactory;
using Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture;
using Bogus;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public class DataFlowRunResultAsJsonTests
    {
        private readonly ITestOutputHelper _outputWriter;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowRunResultAsJsonTests" /> class.
        /// </summary>
        public DataFlowRunResultAsJsonTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public void GetDataAsJson_WithJsonObject_SucceedsByParsing()
        {
            // Arrange
            Arbitrary<string> arb = Arb.From(Gen.Fresh(() => DataPreviewJson.GenerateJsonObject().ToString()));
            Prop.ForAll(arb, expected =>
            {
                var output = DataPreview.Create(Assert.IsType<JsonObject>(JsonNode.Parse(expected))).ToString();
                DataFlowRunResult result = CreateRunResult(output);

                // Act
                JsonNode actual = result.GetDataAsJson();

                // Assert
                AssertJson.Equal(expected, actual.ToString(), opt => opt.MaxInputCharacters = int.MaxValue);

            }).QuickCheckThrowOnFailure(_outputWriter);
        }

        [Fact]
        public void GetDataAsJson_WithJsonArray_SucceedsByParsing()
        {
            // Arrange
            Arbitrary<string> arb = Arb.From(Gen.Fresh(() => DataPreviewJson.GenerateJsonArrayOfObjects(min: 2, max: 3).ToString()));
            Prop.ForAll(arb, expected =>
            {
                var output = DataPreview.Create(Assert.IsType<JsonArray>(JsonNode.Parse(expected))).ToString();
                DataFlowRunResult result = CreateRunResult(output);

                // Act
                JsonNode actual = result.GetDataAsJson();

                // Assert
                AssertJson.Equal(expected, actual.ToString(), opt => opt.MaxInputCharacters = int.MaxValue);

            }).QuickCheckThrowOnFailure(_outputWriter);
        }

        public static IEnumerable<object[]> SucceededSampleDataForSingleDocument
        {
            get
            {
                yield return new object[]
                {
                    "name as string", "[\"sam\"]",
                    "{ \"name\": \"sam\" }"
                };
                yield return new object[]
                {
                    "id as string", "[\"123\"]",
                    "{ \"id\": 123 }"
                };
                yield return new object[]
                {
                    "id as string", "[ null ]",
                    "{ \"id\": null }"
                };
                yield return new object[]
                {
                    "numbers as string[]", "[ [ \"1\", \"2\", \"3\" ] ]",
                    "{ \"numbers\": [ 1, 2, 3 ] }"
                };
                yield return new object[]
                {
                    "numbers as string[]", "[ null ]",
                    "{ \"numbers\": null }"
                };
                yield return new object[]
                {
                    "min as string, max as string", "[ \"1.1\", \"10.2\" ]",
                    "{ \"min\": 1.1, \"max\": 10.2 }",
                    void (DataPreviewJsonOptions options) => options.CultureInfo = CultureInfo.GetCultureInfo("en-US")
                };
                yield return new object[]
                {
                    "min as string, max as string", "[ \"1,1\", \"10,2\" ]",
                    "{ \"min\": 1.1, \"max\": 10.2 }",
                    void (DataPreviewJsonOptions options) => options.CultureInfo = CultureInfo.GetCultureInfo("fr-FR")
                };
                yield return new object[]
                {
                    "{street-name} as string", "[ \"Baker street\" ]",
                    "{ \"street-name\": \"Baker street\" }"
                };
                yield return new object[]
                {
                    "Beatles as string[]", "[ [ \"Lennon\", \"McCartney\", \"Harrison\", \"Starr\" ] ]",
                    "{ \"Beatles\": [ \"Lennon\", \"McCartney\", \"Harrison\", \"Starr\" ] }"
                };
                yield return new object[]
                {
                    "movie as (name as string, director as string, genre as string)",
                    "[ [ \"Beetlejuice\", \"Tim Burton\", \"horror-comedy\" ] ]",
                    "{ \"movie\": { \"name\": \"Beetlejuice\", \"director\": \"Tim Burton\", \"genre\": \"horror-comedy\" } }"
                };
                yield return new object[]
                {
                    "{X-Files} as (firstName as string, lastName as string)[]",
                    "[ [ [ \"Dana\", \"Scully\" ], [ \"Fox\", \"Mulder\" ] ] ]",
                    "{ \"X-Files\": [ { \"firstName\": \"Dana\", \"lastName\": \"Scully\" }, { \"firstName\": \"Fox\", \"lastName\": \"Mulder\" } ] }"
                };
                yield return new object[]
                {
                    "{post-metal} as (bandName as string, related as (bandName as string)[])",
                    "[ [ \"Cult of Luna\", [ [ \"Amenra\" ] ] ] ]",
                    "{ \"post-metal\": { \"bandName\": \"Cult of Luna\", \"related\": [ { \"bandName\": \"Amenra\" } ] } }"
                };
                yield return new object[]
                {
                    "{prog-metal} as (bandName as string, country as string, related as (bandName as string, country as string)[])[]",
                    "[ [ [ \"Wheel\", \"Finland\", [ [ \"Leprous\", \"Norway\" ], [ \"Haken\", \"England\" ] ] ], [ \"Meshuggah\", \"Sweden\", [ [ \"Vildjharta\", \"Sweden\" ] ] ] ] ]",
                    "{ \"prog-metal\": [ { \"bandName\": \"Wheel\", \"country\": \"Finland\", \"related\": [ { \"bandName\": \"Leprous\", \"country\": \"Norway\" }, { \"bandName\": \"Haken\", \"country\": \"England\" } ] }, { \"bandName\": \"Meshuggah\", \"country\": \"Sweden\", \"related\": [ { \"bandName\": \"Vildjharta\", \"country\": \"Sweden\" } ] } ] }"
                };
            }
        }

        [Theory]
        [MemberData(nameof(SucceededSampleDataForSingleDocument))]
        public void GetDataAsJson_WithSampleDataForSingleDocument_SucceedsByParsing(
            string headersTxt,
            string dataTxt,
            string expectedJson,
            Action<DataPreviewJsonOptions> configureOptions = null)
        {
            // Arrange
            var preview = DataPreview.Create(headersTxt, $"[{dataTxt}]");
            DataFlowRunResult result = CreateRunResult(preview);

            // Act
            JsonNode actual = result.GetDataAsJson(configureOptions);

            // Assert
            AssertJson.Equal(AssertJson.Load(expectedJson), actual);
        }

        public static IEnumerable<object[]> SucceededSampleDataForMultipleDocuments
        {
            get
            {
                yield return new object[]
                {
                    "color as string",
                    "[ [ \"Green\" ], [ \"Pink\" ], [ \"Yellow\" ] ]",
                    "[ { \"color\": \"Green\" }, { \"color\": \"Pink\" }, { \"color\": \"Yellow\" } ]"
                };
                yield return new object[]
                {
                    "characterName as string, movieName as string",
                    "[ [ \"Neo\", \"Matrix\" ], [ \"Rick Deckard\", \"Blade Runner\" ] ]",
                    "[ { \"characterName\": \"Neo\", \"movieName\": \"Matrix\" }, { \"characterName\": \"Rick Deckard\", \"movieName\": \"Blade Runner\" } ]"
                };
                yield return new object[]
                {
                    "book as (title as string, author as string)",
                    "[ [ [ \"Ubik\", \"Philip K. Dick\" ] ], [ [ \"Authority\", \"Jeff VanderMeer\" ] ] ]",
                    "[ { \"book\": { \"title\": \"Ubik\", \"author\": \"Philip K. Dick\" } }, { \"book\": { \"title\": \"Authority\", \"author\": \"Jeff VanderMeer\" } } ]"
                };
                yield return new object[]
                {
                    "{classic-reads} as (title as string, author as string)[]",
                    "[ [ [ [ \"Nineteen Eighty-Four\", \"George Orwell\" ] ] ], [ [ [ \"Lord of the Flies\", \"William Golding\" ] ] ] ]",
                    "[ { \"classic-reads\": [ { \"title\": \"Nineteen Eighty-Four\", \"author\": \"George Orwell\" } ] }, { \"classic-reads\": [ { \"title\": \"Lord of the Flies\", \"author\": \"William Golding\" } ] } ]"
                };
            }
        }

        [Theory]
        [MemberData(nameof(SucceededSampleDataForMultipleDocuments))]
        public void GetDataAsJson_WithSampleDataForMultipleDocuments_SucceedsByParsing(string headersTxt, string dataTxt, string expectedJson)
        {
            // Arrange
            var preview = DataPreview.Create(headersTxt, dataTxt);
            DataFlowRunResult result = CreateRunResult(preview);

            // Act
            JsonNode actual = result.GetDataAsJson();

            // Assert
            AssertJson.Equal(AssertJson.Load(expectedJson), actual);
        }

        public static IEnumerable<object[]> FailingInvalidInput
        {
            get
            {
                yield return new object[]
                {
                    "color , , as string",
                    "[ [ \"Green\" ], [ \"Pink\" ], [ \"Yellow\" ] ]"
                };
                yield return new object[]
                {
                    "book as ((title as string)",
                    "[ [ [ \"Ubik\" ] ], [ [ \"Authority\" ] ] ]"
                };
                yield return new object[]
                {
                    "{classic-reads} as (title as string, author as string)[[]]",
                    "[ [ [ [ \"Nineteen Eighty-Four\", \"George Orwell\" ] ] ], [ [ [ \"Lord of the Flies\", \"William Golding\" ] ] ] ]"
                };
                yield return new object[]
                {
                    "movie as (name as string, director as string, genre as string)",
                    "[ [ { \"this\": \"that\" }, \"Tim Burton\", \"horror-comedy\" ] ]",
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingInvalidInput))]
        public void GetDataAsJson_WithInvalidSample_FailsWithDescription(string headersTxt, string dataTxt)
        {
            // Arrange
            var preview = DataPreview.Create(headersTxt, dataTxt);

            // Act / Assert
            var exception = ShouldFailToGetDataAsJson(preview.ToString());
            Assert.Contains("cannot load", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("only supports limited", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void GetDataAsJson_WithBlankInput_Fails(string input)
        {
            ShouldFailToGetDataAsJson(input);
        }

        private static JsonException ShouldFailToGetDataAsJson(string input)
        {
            // Arrange
            DataFlowRunResult result = CreateRunResult(input);

            // Act / Assert
            return Assert.ThrowsAny<JsonException>(() => result.GetDataAsJson());
        }

        private static DataFlowRunResult CreateRunResult(DataPreview preview)
        {
            return CreateRunResult(preview.ToString());
        }

        private static DataFlowRunResult CreateRunResult(string input)
        {
            return new DataFlowRunResult(status: Bogus.Lorem.Word(), BinaryData.FromString(input));
        }

        [Fact]
        public void SetCulture_WithoutValue_Fails()
        {
            // Arrange
            var options = new DataPreviewJsonOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.CultureInfo = null);
        }
    }
}