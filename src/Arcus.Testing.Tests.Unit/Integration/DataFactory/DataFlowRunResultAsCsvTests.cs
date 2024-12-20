using System;
using System.Linq;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Unit.Integration.DataFactory.Fixture;
using Bogus;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Integration.DataFactory
{
    public class DataFlowRunResultAsCsvTests
    {
        private readonly ITestOutputHelper _outputWriter;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowRunResultAsCsvTests" /> class.
        /// </summary>
        public DataFlowRunResultAsCsvTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public void GetDataAsCsv_WithData_SucceedsByParsing()
        {
            // Arrange
            var arb = Arb.From(Gen.Fresh(() => TestCsv.Generate(opt =>
            {
                opt.Header = AssertCsvHeader.Present;
                opt.Separator = ';';
                opt.NewLine = Environment.NewLine;
                opt.Quote = '\'';
            }).ToString()));
            Prop.ForAll(arb, csv =>
            {
                var expected = AssertCsv.Load(csv);
                var preview = DataPreview.Create(expected);
                DataFlowRunResult result = CreateRunResult(preview.ToString());

                // Act
                CsvTable actual = result.GetDataAsCsv();

                // Assert
                AssertCsv.Equal(expected, actual);
            }).QuickCheckThrowOnFailure(_outputWriter);
        }

        [Theory]
        [InlineData(
            "{ \"output\": { \"schema\": \"output(id as string, name as string)\", \"data\": [ [ \"1\", \"this\" ], [ \"2\", \"that\" ] ] } }",
            "id,name\n\r1,this\n\r2,that", "\n\r", ',')]
        [InlineData(
            "{ \"output\": { \"schema\": \"output(first_name as string, last_name as string)\", \"data\": [ \"Howard\", \"Lovecraft\" ] } }",
            "first_name;last_name\nHoward;Lovecraft", "\n", ';')]
        [InlineData(
            "{ \"output\": { \"schema\": \"output(product as string, price as (value as string, unit as string)[])\", \"data\": [ [ \"pc\", \"1000,euro\" ], [ \"printer\", \"500,euro\" ] ] } }",
            "product;price\npc;1000,euro\nprinter;500,euro", "\n", ';')]
        public void GetDataAsCsv_WithSampleData_SucceedsByParsing(string json, string expectedCsv, string newLine, char separator)
        {
            // Act
            CsvTable actual = GetDataAsCsv(json, newLine, separator);

            // Assert
            CsvTable expected = AssertCsv.Load(expectedCsv, opt =>
            {
                opt.NewLine = newLine;
                opt.Separator = separator;
            });
            AssertCsv.Equal(expected, actual);
        }

        [Theory]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string, name as string)\", \"data\": [] } }", "\"id\",\"name\"")]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string, names as (this as string, that as string)[])\", \"data\": [] } }", "\"id\",\"names\"")]
        [InlineData("{ \"output\": { \"schema\": \"output(id1 as string, first_lastname as (first as (name as string)[], last as (name as string)[]))[]\", \"data\": [] } }", "\"id1\",\"first_lastname\"")]
        [InlineData(
            "{ \"output\": { \"schema\": \"output(id as string, LegalEntityId as string, CustomerNumber as string, LastName as string, Prefix as string, FirstName as string, Gender as string, LanguageCode as string, Emails as (Address as string, Type as string)[], TelephoneNumbers as (Number as string, Type as string)[], Addresses as (Type as string, Street as string, HouseNumber as string, HouseNumberAddition as string, ZipCode as string, City as string, CountryCode as string)[])\", \"data\": [] } }",
            "\"id\",\"LegalEntityId\",\"CustomerNumber\",\"LastName\",\"Prefix\",\"FirstName\",\"Gender\",\"LanguageCode\",\"Emails\",\"TelephoneNumbers\",\"Addresses\"")]
        public void GetDataAsCsv_WithSampleSchema_SucceedsByParsing(string json, string expectedTxt)
        {
            // Act
            CsvTable csv = GetDataAsCsv(json);

            // Assert
            Assert.Equal(expectedTxt, string.Join(",", csv.HeaderNames.Select(h => $"\"{h}\"")));
        }

        private static CsvTable GetDataAsCsv(string json, string newLine = "\n", char separator = ';')
        {
            string status = Bogus.Lorem.Word();
            var result = new DataFlowRunResult(status, BinaryData.FromString(json));
            Assert.Equal(status, result.Status);

            return result.GetDataAsCsv(opt =>
            {
                opt.NewLine = newLine;
                opt.Separator = separator;
            });
        }

        [Theory]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string)\", \"data\": null } }")]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string)\", \"data\": { } } }")]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string)\", \"data\": \"\" } }")]
        [InlineData("{ \"output\": { \"schema\": \"output(id as string)\", \"data\": 1 } }")]
        public void GetDataAsCsv_WithoutData_Fails(string json)
        {
            CsvException exception = ShouldFailToGetDataAsCsv(json);
            ShouldContain(exception.Message, "output.data", "node");
        }

        [Theory]
        [InlineData("{ \"output\": {  } }")]
        [InlineData("{ \"output\": { \"schema\": null } }")]
        [InlineData("{ \"output\": { \"schema\": \"\" } }")]
        [InlineData("{ \"output\": { \"schema\": \"  \" } }")]
        [InlineData("{ \"output\": { \"schema\": \"output()\" } }")]
        [InlineData("{ \"output\": { \"schema\": 2 } }")]
        public void GetDataAsCsv_WithoutSchema_Fails(string json)
        {
            CsvException exception = ShouldFailToGetDataAsCsv(json);
            ShouldContain(exception.Message, "output.schema", "node");
        }

        [Theory]
        [InlineData("{ \"output\": null }")]
        [InlineData("{ \"output\": \"\" }")]
        [InlineData("{ \"output\": \"  \" }")]
        [InlineData("{ \"output\": [] }")]
        [InlineData("{ \"output\": 5 }")]
        public void GetDataAsCsv_WithoutOutputNode_Fails(string json)
        {
            CsvException exception = ShouldFailToGetDataAsCsv(json);
            ShouldContain(exception.Message, "output", "node");
        }

        [Theory]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("[ { \"this\": \"that\" } ]")]
        [InlineData("<xml/>")]
        [InlineData("one;two;three")]
        public void GetDataAsCsv_WithOtherThanJsonbObject_Fails(string json)
        {
            ShouldFailToGetDataAsCsv(json);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void GetDataAsCsv_WithBlankInput_Fails(string input)
        {
            ShouldFailToGetDataAsCsv(input);
        }

        private static CsvException ShouldFailToGetDataAsCsv(string input)
        {
            // Arrange
            DataFlowRunResult result = CreateRunResult(input);

            // Act / Assert
            return Assert.ThrowsAny<CsvException>(() => result.GetDataAsCsv());
        }

        private static void ShouldContain(string actual, params string[] expected)
        {
            Assert.All(expected, str => Assert.Contains(str, actual, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Create_WithoutStatus_Fails(string status)
        {
            // Arrange
            BinaryData data = BinaryData.FromString("{ \"output\": { \"schema\": \"output()\", \"data\": [] } }");

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new DataFlowRunResult(status, data));
        }

        private static DataFlowRunResult CreateRunResult(string input)
        {
            return new DataFlowRunResult(status: Bogus.Lorem.Word(), BinaryData.FromString(input));
        }
    }
}
