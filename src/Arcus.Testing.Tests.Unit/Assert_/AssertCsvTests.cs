using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertCsvTests
    {
        private static readonly Faker Bogus = new();
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertCsvTests" /> class.
        /// </summary>
        public AssertCsvTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Property]
        public void Compare_WithSameCsv_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual);
        }

        [Property]
        public void CompareWithIgnoreOrder_WithShuffledActual_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.Shuffle();

            // Act / Assert
            EqualCsv(expected, actual, opt => opt.Order = AssertCsvOrder.Ignore);
        }

        [Property]
        public void CompareWithIncludeLineOrder_WithDifferentCellValue_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string changeValue = actual.ChangeCellValue();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, opt => opt.Order = AssertCsvOrder.Include, "different value", changeValue);
        }

        [Property]
        public void CompareWithIgnoreLineOrder_WithDifferentCellValue_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.LineCount = Bogus.Random.Int(2, 10));
            TestCsv actual = expected.Copy();

            string changeValue = actual.ChangeCellValue();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, opt => opt.Order = AssertCsvOrder.Ignore, "does not contain a line", changeValue);
        }

        [Property]
        public void Compare_WithDifferentColumnName_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string originalValue = actual.ChangeColumnValue();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, "missing", "column", originalValue);
        }

        [Property]
        public void Compare_WithDifferentLineLength_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.AddLine();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, "has", actual.LineCount.ToString(), "lines", expected.LineCount.ToString());
        }

        [Property]
        public void Compare_WithDifferentColumnLength_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.AddColumn();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, "has", actual.ColumnCount.ToString(), "columns", expected.ColumnCount.ToString());
        }

        [Property]
        public void Compare_WithDiffCsv_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = TestCsv.Generate();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual);
        }

        public static IEnumerable<object[]> SucceedingBeEquivalentCases
        {
            get
            {
                yield return new object[]
                {
                    $"duplicate;duplicate;works{Environment.NewLine}1;lorem;3",
                    $"duplicate;duplicate;works{Environment.NewLine}1;lorem;3"
                };
                yield return new object[]
                {
                    $"total{Environment.NewLine}100,02",
                    $"total{Environment.NewLine}100,020"
                };
                yield return new object[]
                {
                    $"trailing;semicolon;works;{Environment.NewLine}foo;bar;10;",
                    $"trailing;semicolon;works;{Environment.NewLine}foo;bar;10;"
                };
                yield return new object[]
                {
                    $"product;amount{Environment.NewLine}pc;1{Environment.NewLine}printer;2",
                    $"product;amount{Environment.NewLine}printer;2{Environment.NewLine}pc;1",
                    AssertCsvOrder.Ignore
                };
                yield return new object[]
                {
                    $"name;category{Environment.NewLine}pc;IT{Environment.NewLine}pc;IT{Environment.NewLine}printer;Infra",
                    $"name;category{Environment.NewLine}pc;IT{Environment.NewLine}printer;Infra{Environment.NewLine}pc;IT",
                    AssertCsvOrder.Ignore
                };
            }
        }

        [Theory]
        [MemberData(nameof(SucceedingBeEquivalentCases))]
        public void Compare_Equal_ShouldSucceed(string expectedCsv, string actualCsv, AssertCsvOrder? order = null)
        {
            EqualCsv(expectedCsv, actualCsv, opt => opt.Order = order ?? opt.Order);
        }

        public static IEnumerable<object[]> FailingBeEquivalentCases
        {
            get
            {
                yield return new object[]
                {
                    "id", 
                    "ids", 
                    "missing", "column", "id"
                };
                yield return new object[]
                {
                    "id",
                    "id;name",
                    "has 2 columns instead of 1"
                };
                yield return new object[]
                {
                    $"id;name{Environment.NewLine}0;Arcus",
                    "id;category",
                    "has 0 lines instead of 1"
                };
                yield return new object[]
                {
                    $"description;cost;total{Environment.NewLine}lorem;100;123.1",
                    $"description;cost;total{Environment.NewLine}lorem;101;123.1",
                    "different", "value", "100 while actual 101"
                };
                yield return new object[]
                {
                    $"name;category{Environment.NewLine}pc;IT{Environment.NewLine}pc;IT{Environment.NewLine}printer;Infra",
                    $"name;category{Environment.NewLine}pc;IT{Environment.NewLine}printer;Infra{Environment.NewLine}printer;Infra",
                    "different", "value", "pc while actual printer", "line number 1"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_NotEqual_ShouldFailWithDifference(string expectedCsv, string actualCsv, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(expectedCsv, actualCsv, expectedDifferences);
        }

        private void CompareShouldFailWithDescription(TestCsv expected, TestCsv actual, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(expected, actual, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDescription(TestCsv expected, TestCsv actual, Action<AssertCsvOptions> configureOptions, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => EqualCsv(expected, actual, configureOptions));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.Contains("CSV contents", exception.Message);
            Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
        }

        private static void CompareShouldFailWithDescription(string expectedCsv, string actualCsv, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(expectedCsv, actualCsv, configureOptions: null, expectedDifferences);
        }

        private static void CompareShouldFailWithDescription(string expectedCsv, string actualCsv, Action<AssertCsvOptions> configureOptions, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => EqualCsv(expectedCsv, actualCsv, configureOptions));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.Contains("CSV contents", exception.Message);
            Assert.All(expectedDifferences, expectedDifference => Assert.Contains(expectedDifference, exception.Message));
        }

        private static void EqualCsv(string expectedCsv, string actualCsv, Action<AssertCsvOptions> configureOptions = null)
        {
            AssertCsv.Equal(expectedCsv, actualCsv, opt =>
            {
                opt.MaxInputCharacters = int.MaxValue;
                configureOptions?.Invoke(opt);
            });
        }

        private void EqualCsv(TestCsv expected, TestCsv actual, Action<AssertCsvOptions> configureOptions = null)
        {
            CsvDocument expectedDoc = Load(expected, tag: "Expected");
            CsvDocument actualDoc = Load(actual, tag: "Actual");
            
            AssertCsv.Equal(expectedDoc, actualDoc, configureOptions: options =>
            {
                options.MaxInputCharacters = int.MaxValue;
                configureOptions?.Invoke(options);
            });
        }

        [Property]
        public void Load_WithLineWithDiffLength_FailsWithDescription()
        {
            // Arrange
            var csv = TestCsv.Generate();
            csv.AddInvalidLine();

            // Act / Assert
            LoadShouldFailWithDescription(csv, csv.ColumnCount.ToString(), "columns", "amount");
        }

        [Property]
        public void Load_WithRandomCsvWithHeader_Succeeds()
        {
            // Arrange
            TestCsv csv = TestCsv.Generate();
            
            // Act
            CsvDocument doc = Load(csv);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(csv.HeaderNames, doc.HeaderNames);
            Assert.Equal(csv.LineCount, doc.Lines.Length);

            Assert.All(csv.Lines.Skip(1).Zip(doc.Lines), line =>
            {
                Assert.All(line.First.Zip(line.Second.Cells), cell =>
                {
                    Assert.Equal(cell.First, cell.Second.Value);
                });
            });
        }

        [Property]
        public void Load_WithRandomCsvWithoutHeader_Succeeds()
        {
            // Arrange
            TestCsv csv = TestCsv.Generate(opt => opt.Header = CsvHeader.Missing);

            // Act
            CsvDocument doc = Load(csv, opt => opt.Header = CsvHeader.Missing);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(csv.HeaderNames, doc.HeaderNames);

            Assert.Equal(csv.LineCount, doc.Lines.Length);
            Assert.All(csv.Lines.Zip(doc.Lines), line =>
            {
                Assert.All(line.First.Zip(line.Second.Cells), cell =>
                {
                    Assert.Equal(cell.First, cell.Second.Value);
                });
            });
        }

        [Property]
        public void LoadWithExpectingHeader_WithRandomCsvWithoutHeader_SucceedsWithEmptyLines()
        {
            // Arrange
            TestCsv csv = TestCsv.Generate(opt =>
            {
                opt.Header = CsvHeader.Missing;
                opt.LineCount = 1;
            });

            // Act
            CsvDocument doc = Load(csv, opt => opt.Header = CsvHeader.Present);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(0, doc.LineCount);
        }

        [Property]
        public void Compare_WithoutLines_SucceedsWithEmptyLines()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.LineCount = 0);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public void LoadConfiguredMissingHeader_WithBlankSingleCell_SucceedsWithSingeLine(string csv)
        {
            // Act
            CsvDocument doc = Load(csv, opt => opt.Header = CsvHeader.Missing);

            // Assert
            Assert.Equal(1, doc.LineCount);
            Assert.Equal(1, doc.ColumnCount);

            CsvLine line = Assert.Single(doc.Lines);
            CsvCell cell = Assert.Single(line.Cells);
            Assert.Equal(csv, cell.Value);
            Assert.Equal(0, cell.ColumnNumber);
            Assert.Equal(0, cell.LineNumber);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public void LoadConfiguredPresentHeader_WithBlankSingleCell_SucceedsWithEmptyLines(string csv)
        {
            // Act
            CsvDocument doc = Load(csv, opt => opt.Header = CsvHeader.Present);

            // Assert
            Assert.Equal(0, doc.LineCount);
            Assert.Equal(1, doc.ColumnCount);
            Assert.Empty(doc.Lines);
        }

        private void LoadShouldFailWithDescription(TestCsv csv, params string[] expectedFailures)
        {
            var exception = Assert.ThrowsAny<CsvException>(() => Load(csv));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.All(expectedFailures, expectedFailure => Assert.Contains(expectedFailure, exception.Message));
        }

        private CsvDocument Load(TestCsv csv, Action<AssertCsvOptions> configureOptions = null, string tag = "Input")
        {
            return Load(csv.ToString(), tag: tag, configureOptions: options =>
            {
                options.NewLine = csv.NewLine;
                options.Separator = csv.Separator;

                configureOptions?.Invoke(options);
            });
        }

        private CsvDocument Load(string csv, Action<AssertCsvOptions> configureOptions = null,  string tag = "Input")
        {
            _outputWriter.WriteLine("{0}: {1}", Environment.NewLine + tag, csv + Environment.NewLine);

            return AssertCsv.Load(csv, configureOptions);
        }
    }
}
