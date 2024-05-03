using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Xunit;
using Xunit.Abstractions;
using static System.DateTimeOffset;
using static System.Environment;

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
        public void CompareWithIgnoredOrderAndHeaders_WithSameCsv_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();
            string[] ignoredHeaderNames = ChangeDifferentCellValues(actual);

            actual.ShuffleRows();
            actual.ShuffleColumns();

            EqualCsv(expected, actual, options =>
            {
                options.ColumnOrder = AssertCsvOrder.Ignore;
                options.RowOrder = AssertCsvOrder.Ignore;
                Assert.All(ignoredHeaderNames, n => options.IgnoreColumn(n));
            });
        }

        [Property]
        public void CompareWithIgnoredColumn_WithDuplicateColumn_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string duplicateColumn = Bogus.PickRandom(expected.HeaderNames);
            actual.AddColumn(duplicateColumn);
            expected.AddColumn(duplicateColumn);

            EqualCsv(expected, actual, options =>
            {
                options.ColumnOrder = AssertCsvOrder.Ignore;
                options.IgnoreColumn(duplicateColumn);
            });
        }

        [Property]
        public void CompareWithIgnoredColumnOrder_WithDuplicateColumn_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string duplicateColumn = Bogus.PickRandom(expected.HeaderNames);
            actual.AddColumn(duplicateColumn);
            expected.AddColumn(duplicateColumn);

            CompareShouldFailWithDescription(expected, actual, options => options.ColumnOrder = AssertCsvOrder.Ignore, 
                "cannot compare", AssertCsvOrder.Ignore.ToString(), "duplicate", "columns", duplicateColumn);
        }

        [Property]
        public void Compare_WithPresentExpectedHeaderAndMissingActualHeader_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(options => options.Header = AssertCsvHeader.Present);
            CsvTable expectedCsv = Load(expected);

            TestCsv actual = expected.Copy();
            actual.RemoveHeaders();
            CsvTable actualCsv = Load(actual, options => options.Header = AssertCsvHeader.Missing);

            // Act / Assert
            CompareShouldFailWithDescription(expectedCsv, actualCsv, configureOptions: null, "CSV header", "configured");
        }

        [Property]
        public void Compare_WithIgnoreAllColumns_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            ChangeDifferentCellValues(actual);

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                Assert.All(expected.HeaderNames, n => options.IgnoreColumn(n));
            });
        }

        [Property]
        public void CompareWithIgnoredColumn_WithDifferentColumn_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string[] ignoredHeaderNames = ChangeDifferentCellValues(actual);

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                Assert.All(ignoredHeaderNames, name => options.IgnoreColumn(name));
            });
        }

        private static string[] ChangeDifferentCellValues(TestCsv csv)
        {
            return Enumerable.Repeat(csv, Bogus.Random.Int(1, 10))
                             .Select(x => x.ChangeCellValue().headerName)
                             .ToArray();
        }

        [Property]
        public void CompareWithIgnoredOrder_WithShuffledActual_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.ShuffleRows();
            actual.ShuffleColumns();

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                options.ColumnOrder = AssertCsvOrder.Ignore;
                options.RowOrder = AssertCsvOrder.Ignore;
            });
        }

        [Property]
        public void CompareWithIgnoredColumnOrder_WithShuffledActual_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.ShuffleColumns();
            
            // Act / Assert
            EqualCsv(expected, actual, options => options.ColumnOrder = AssertCsvOrder.Ignore);
        }

        [Property]
        public void CompareWithIgnoreRowOrder_WithShuffledActual_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.ShuffleRows();

            // Act / Assert
            EqualCsv(expected, actual, opt => opt.RowOrder = AssertCsvOrder.Ignore);
        }

        [Property]
        public void CompareWithoutHeader_WithSameCsv_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(
                Load(expected, opt => opt.Header = AssertCsvHeader.Missing), 
                Load(actual, opt => opt.Header = AssertCsvHeader.Missing));
        }

        [Property]
        public void CompareWithIncludeRowOrder_WithDifferentCellValue_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            (_, string changeValue) = actual.ChangeCellValue();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, opt => opt.RowOrder = AssertCsvOrder.Include, "different value", changeValue);
        }

        [Property]
        public void CompareWithIgnoreRowOrder_WithDifferentCellValue_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.RowCount = Bogus.Random.Int(2, 10));
            TestCsv actual = expected.Copy();

            (_, string changeValue) = actual.ChangeCellValue();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, opt => opt.RowOrder = AssertCsvOrder.Ignore, "does not contain a row", changeValue);
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
        public void Compare_WithDifferentRowLength_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.AddRow();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, "has", actual.RowCount.ToString(), "rows", expected.RowCount.ToString());
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
                    $"duplicate;duplicate;works{NewLine}1;lorem;3",
                    $"duplicate;duplicate;works{NewLine}1;lorem;3"
                };
                yield return new object[]
                {
                    $"total{NewLine}100,02",
                    $"total{NewLine}100,020"
                };
                yield return new object[]
                {
                    $"trailing;semicolon;works;{NewLine}foo;bar;10;",
                    $"trailing;semicolon;works;{NewLine}foo;bar;10;"
                };
                yield return new object[]
                {
                    $"product;amount{NewLine}pc;1{NewLine}printer;2",
                    $"product;amount{NewLine}printer;2{NewLine}pc;1",
                    void (AssertCsvOptions options) => options.RowOrder = AssertCsvOrder.Ignore
                };
                yield return new object[]
                {
                    $"name;category{NewLine}pc;IT{NewLine}pc;IT{NewLine}printer;Infra",
                    $"name;category{NewLine}pc;IT{NewLine}printer;Infra{NewLine}pc;IT",
                    void (AssertCsvOptions options) => options.RowOrder = AssertCsvOrder.Ignore
                };
                yield return new object[]
                {
                    $"a;b;time;c{NewLine}1;2;{Now};3",
                    $"a;b;time;c{NewLine}1;2;{UtcNow};3", 
                    void (AssertCsvOptions options) => options.IgnoreColumn("time")
                };
                yield return new object[]
                {
                    $"a;date;time{NewLine}1;{Now};{Now.TimeOfDay}",
                    $"a;date;time{NewLine}1;{Now};{Now.TimeOfDay}",
                    void (AssertCsvOptions options) => options.IgnoreColumn("date").IgnoreColumn("time")
                };
                yield return new object[]
                {
                    $"a;time;date;date{NewLine}1;{Now};{Now};{Now}",
                    $"a;time;date;date{NewLine}1;{Now};{Now};{Now}",
                    void (AssertCsvOptions options) => options.IgnoreColumn("date")
                };
                yield return new object[]
                {
                    $"a;b;c;d{NewLine}1;2;3;4{NewLine}5;6;7;8{NewLine}9;10;11;12",
                    $"a;d;c;b{NewLine}1;4;3;2{NewLine}5;8;7;6{NewLine}9;12;11;10",
                    void (AssertCsvOptions options) => options.ColumnOrder = AssertCsvOrder.Ignore
                };
                yield return new object[]
                {
                    $"a;b;c;d{NewLine}0;1;0;1{NewLine}1;0;0;1{NewLine}0;0;1;1",
                    $"a;b;d;c{NewLine}1;0;1;0{NewLine}0;1;1;0{NewLine}0;0;1;1",
                    void (AssertCsvOptions options) =>
                    {
                        options.ColumnOrder = AssertCsvOrder.Ignore;
                        options.RowOrder = AssertCsvOrder.Ignore;
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(SucceedingBeEquivalentCases))]
        public void Compare_Equal_ShouldSucceed(string expectedCsv, string actualCsv, Action<AssertCsvOptions> configureOptions = null)
        {
            EqualCsv(expectedCsv, actualCsv, configureOptions);
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
                    $"a;b;c;d{NewLine}1;2;3;4",
                    $"b;a;c;d{NewLine}1;2;3;4",
                    "missing", "column: a"
                };
                yield return new object[]
                {
                    "id",
                    "id;name",
                    "has 2 columns instead of 1"
                };
                yield return new object[]
                {
                    $"id;name{NewLine}0;Arcus",
                    "id;category",
                    "has 0 rows instead of 1"
                };
                yield return new object[]
                {
                    $"description;cost;total{NewLine}lorem;100;123.1",
                    $"description;cost;total{NewLine}lorem;101;123.1",
                    "different", "value", "100 while actual 101"
                };
                yield return new object[]
                {
                    $"name;category{NewLine}pc;IT{NewLine}pc;IT{NewLine}printer;Infra",
                    $"name;category{NewLine}pc;IT{NewLine}printer;Infra{NewLine}printer;Infra",
                    "different", "value", "pc while actual printer", "row number 1"
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_NotEqual_ShouldFailWithDifference(string expectedCsv, string actualCsv, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(expectedCsv, actualCsv, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDescription(TestCsv expected, TestCsv actual, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(expected, actual, configureOptions: null, expectedDifferences);
        }

        private void CompareShouldFailWithDescription(TestCsv expected, TestCsv actual, Action<AssertCsvOptions> configureOptions, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => EqualCsv(expected, actual, configureOptions));
            AssertFailureDescription(expectedDifferences, exception);
        }

        private static void CompareShouldFailWithDescription(string expectedCsv, string actualCsv, Action<AssertCsvOptions> configureOptions = null, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => EqualCsv(expectedCsv, actualCsv, configureOptions));
            AssertFailureDescription(expectedDifferences, exception);
        }

        private void CompareShouldFailWithDescription(CsvTable expectedCsv, CsvTable actualCsv, Action<AssertCsvOptions> configureOptions, params string[] expectedDifferences)
        {
            var exception = Assert.ThrowsAny<AssertionException>(() => EqualCsv(expectedCsv, actualCsv, configureOptions));
            AssertFailureDescription(expectedDifferences, exception);
        }

        private static void AssertFailureDescription(string[] expectedDifferences, AssertionException exception)
        {
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
            CsvTable expectedCsv = Load(expected, tag: "Expected");
            CsvTable actualCsv = Load(actual, tag: "Actual");

            EqualCsv(expectedCsv, actualCsv, configureOptions);
        }

        private void EqualCsv(CsvTable expected, CsvTable actual, Action<AssertCsvOptions> configureOptions = null)
        {
            AssertCsv.Equal(expected, actual, configureOptions: options =>
            {
                options.MaxInputCharacters = int.MaxValue;
                configureOptions?.Invoke(options);
            });
        }

        [Property]
        public void Load_WithRowWithDiffLength_FailsWithDescription()
        {
            // Arrange
            var csv = TestCsv.Generate();
            csv.AddInvalidRow();

            // Act / Assert
            LoadShouldFailWithDescription(csv, csv.ColumnCount.ToString(), "columns", "amount");
        }

        [Property]
        public void Load_WithRandomCsvWithHeader_Succeeds()
        {
            // Arrange
            TestCsv csv = TestCsv.Generate();
            
            // Act
            CsvTable doc = Load(csv);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(csv.HeaderNames, doc.HeaderNames);
            Assert.Equal(csv.RowCount, doc.Rows.Length);

            Assert.All(csv.Rows.Skip(1).Zip(doc.Rows), line =>
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
            TestCsv csv = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);

            // Act
            CsvTable doc = Load(csv, opt => opt.Header = AssertCsvHeader.Missing);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(csv.HeaderNames, doc.HeaderNames);

            Assert.Equal(csv.RowCount, doc.Rows.Length);
            Assert.All(csv.Rows.Zip(doc.Rows), line =>
            {
                Assert.All(line.First.Zip(line.Second.Cells), cell =>
                {
                    Assert.Equal(cell.First, cell.Second.Value);
                });
            });
        }

        [Property]
        public void LoadWithExpectingHeader_WithRandomCsvWithoutHeader_SucceedsWithEmptyRows()
        {
            // Arrange
            TestCsv csv = TestCsv.Generate(opt =>
            {
                opt.Header = AssertCsvHeader.Missing;
                opt.RowCount = 1;
            });

            // Act
            CsvTable doc = Load(csv, opt => opt.Header = AssertCsvHeader.Present);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(0, doc.RowCount);
        }

        [Property]
        public void Compare_WithoutRows_SucceedsWithEmptyRows()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.RowCount = 0);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public void LoadConfiguredMissingHeader_WithBlankSingleCell_SucceedsWithSingeRow(string csv)
        {
            // Act
            CsvTable doc = Load(csv, opt => opt.Header = AssertCsvHeader.Missing);

            // Assert
            Assert.Equal(1, doc.RowCount);
            Assert.Equal(1, doc.ColumnCount);

            CsvRow line = Assert.Single(doc.Rows);
            CsvCell cell = Assert.Single(line.Cells);
            Assert.Equal(csv, cell.Value);
            Assert.Equal(0, cell.ColumnNumber);
            Assert.Equal(0, cell.RowNumber);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public void LoadConfiguredPresentHeader_WithBlankSingleCell_SucceedsWithEmptyRows(string csv)
        {
            // Act
            CsvTable doc = Load(csv, opt => opt.Header = AssertCsvHeader.Present);

            // Assert
            Assert.Equal(0, doc.RowCount);
            Assert.Equal(1, doc.ColumnCount);
            Assert.Empty(doc.Rows);
        }

        private void LoadShouldFailWithDescription(TestCsv csv, params string[] expectedFailures)
        {
            var exception = Assert.ThrowsAny<CsvException>(() => Load(csv));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.All(expectedFailures, expectedFailure => Assert.Contains(expectedFailure, exception.Message));
        }

        private CsvTable Load(TestCsv csv, Action<AssertCsvOptions> configureOptions = null, string tag = "Input")
        {
            return Load(csv.ToString(), tag: tag, configureOptions: options =>
            {
                options.NewLine = csv.NewLine;
                options.Separator = csv.Separator;

                configureOptions?.Invoke(options);
            });
        }

        private CsvTable Load(string csv, Action<AssertCsvOptions> configureOptions = null,  string tag = "Input")
        {
            _outputWriter.WriteLine("{0}: {1}", NewLine + tag, csv + NewLine);

            return configureOptions is null 
                ? AssertCsv.Load(csv) 
                : AssertCsv.Load(csv, configureOptions);
        }
    }
}
