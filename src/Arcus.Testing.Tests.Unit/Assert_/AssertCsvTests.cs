﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Arcus.Testing.Failure;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using Bogus;
using FsCheck.Xunit;
using Microsoft.FSharp.Core;
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
        public void Compare_WithoutRows_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.RowCount = 0);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual);
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

        [Property]
        public void CompareWithoutHeader_WithSameCsv_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual, opt => opt.Header = AssertCsvHeader.Missing);
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
        public void CompareWithFloatWithTrailingZeros_WithEscapedComma_StillSucceeds()
        {
            // Arrange
            var commaFloatCulture = CultureInfo.GetCultureInfo("nl-NL");
            TestCsv expected = TestCsv.Generate(opt =>
            {
                opt.CultureInfo = commaFloatCulture;
                opt.Separator = ',';
            });
            TestCsv actual = expected.Copy();

            (int row, int col) = expected.GetRandomCellIndex();

            string trailingZeros = string.Concat(Bogus.Make(Bogus.Random.Int(1, 10), () => "0"));
            string value = Bogus.Random.Float().ToString("F10", commaFloatCulture).Replace(",", "\\,");
            expected.ChangeCellValue(row, col, value);
            actual.ChangeCellValue(row, col, value + trailingZeros);

            // Act / Assert
            EqualCsv(expected, actual, opt =>
            {
                opt.CultureInfo = commaFloatCulture;
                opt.Separator = ',';
            });
        }

        [Property]
        public void CompareWithInteger_WithSameTrailingSpaces_StillSucceedsByHandlingLikeText()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            (int row, int col) = expected.GetRandomCellIndex();
            string value = Bogus.Random.Int().ToString();

            string spaces = RandomSpaces();
            expected.ChangeCellValue(row, col, value + spaces);
            actual.ChangeCellValue(row, col, value + spaces);

            // Act / Assert
            EqualCsv(expected, actual, opt => opt.CultureInfo = CultureInfo.InvariantCulture);
        }

        [Property]
        public void CompareWithInteger_WithDiffTrailingSpaces_FailsSinceHandledLikeText()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            (int row, int col) = expected.GetRandomCellIndex();
            string value = Bogus.Random.Int().ToString();

            expected.ChangeCellValue(row, col, value + RandomSpaces(1, 5));
            actual.ChangeCellValue(row, col, value + RandomSpaces(6, 10));

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, "different", "value", value);
        }

        private static string RandomSpaces(int min = 1, int max = 20)
        {
            return string.Concat(Bogus.Make(Bogus.Random.Int(min, max), () => " "));
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
        public void CompareWithIgnoredRowAndColumnOrder_WithShuffledCsv_Succeeds()
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
        public void CompareWithIgnoredColumnOrderAndColumnIndexes_WithShuffledCsv_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            actual.ShuffleColumns();

            CompareShouldFailWithDescription(
                expected,
                actual,
                options =>
                {
                    options.ColumnOrder = AssertCsvOrder.Ignore;
                    options.IgnoreColumn(expected.IgnoredIndex);
                },
                "cannot compare", "indexes", "column order", "included",
                nameof(AssertCsvOptions.IgnoreColumn), AssertCsvOrder.Ignore.ToString()
            );
        }

        [Property]
        public void CompareWithIgnoredColumnIndex_WithSameCsv_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual, options => options.IgnoreColumn(expected.IgnoredIndex));
        }

        [Property]
        public void CompareWithIgnoredColumnIndexAndMissingHeaders_WithSameCsv_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                options.IgnoreColumn(expected.IgnoredIndex);
                options.Header = AssertCsvHeader.Missing;
            });
        }

        [Property]
        public void CompareWithIgnoredColumnIndexAndIgnoredColumnHeader_WithSameCsvAndIndexIsSameAsHeader_StillSucceeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            var ignoredIndex = expected.IgnoredIndex;
            var headerName = expected.HeaderNames[ignoredIndex];

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                options.IgnoreColumn(ignoredIndex);
                options.IgnoreColumn(headerName);
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
        public void CompareWithIgnoredColumnOrder_WithIgnoredColumn_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate();
            TestCsv actual = expected.Copy();

            string extraColumn = Bogus.PickRandom(expected.HeaderNames);
            actual.AddColumn(extraColumn);
            expected.AddColumn(extraColumn);

            CompareShouldFailWithDescription(expected, actual, options => options.ColumnOrder = AssertCsvOrder.Ignore,
                "cannot compare", AssertCsvOrder.Ignore.ToString(), "duplicate", "columns", extraColumn);
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
        public void Compare_WithPresentExpectedHeaderAndMissingActualHeader_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(options => options.Header = AssertCsvHeader.Present);
            CsvTable expectedCsv = LoadCsv(expected);

            TestCsv actual = expected.Copy();
            actual.RemoveHeaders();
            CsvTable actualCsv = LoadCsv(actual, options => options.Header = AssertCsvHeader.Missing);

            // Act / Assert
            CompareShouldFailWithDescription(expectedCsv, actualCsv, configureOptions: null, "CSV header", "configured");
        }

        [Property]
        public void CompareWithMissingHeader_WithIgnoredColumn_FailsWithDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, opt =>
            {
                opt.Header = AssertCsvHeader.Missing;
                opt.IgnoreColumn(Bogus.Database.Column());
            }, "ignore", "column", "header names", "present");
        }

        private static string[] ChangeDifferentCellValues(TestCsv csv)
        {
            return Enumerable.Repeat(csv, Bogus.Random.Int(1, 10))
                             .Select(x => x.ChangeCellValue().headerName)
                             .ToArray();
        }

        [Property]
        public void CompareWithoutHeader_WithIgnoredColumn_FailsWithMissingHeadersDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            actual.ShuffleRows();
            actual.ShuffleColumns();

            // Act / Assert
            CompareShouldFailWithDescription(expected, actual, options =>
            {
                options.Header = AssertCsvHeader.Missing;
                options.ColumnOrder = AssertCsvOrder.Ignore;
            }, "columns can only be ignored", "header names", "present");
        }

        [Property]
        public void CompareWithoutHeader_WithIgnoredRow_FailsWithMissingHeadersDescription()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);
            TestCsv actual = expected.Copy();

            actual.ShuffleRows();

            // Act / Assert
            EqualCsv(expected, actual, options =>
            {
                options.Header = AssertCsvHeader.Missing;
                options.RowOrder = AssertCsvOrder.Ignore;
            });
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
                    $"total{NewLine}100,020",
                    void (AssertCsvOptions options) => options.CultureInfo = CultureInfo.GetCultureInfo("fr-FR")
                };
                yield return new object[]
                {
                    $"total{NewLine}123.1330",
                    $"total{NewLine}123.133000",
                    void (AssertCsvOptions options) => options.CultureInfo = CultureInfo.GetCultureInfo("en-US")
                };
                yield return new object[]
                {
                    $"product,total cost{NewLine}printer,123\\,450",
                    $"product,total cost{NewLine}printer,123\\,45",
                    void (AssertCsvOptions options) =>
                    {
                        options.Separator = ',';
                        options.Escape = '\\';
                        options.CultureInfo = CultureInfo.GetCultureInfo("nl-NL");
                    }
                };
                yield return new object[]
                {
                    $"a;b;c{NewLine}\"this is a sentence with separator ; in quotes\";100,00;foo",
                    $"a;b;c{NewLine}\"this is a sentence with separator ; in quotes\";100,00;foo"
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
                yield return new object[]
                {
                    $"a;b{NewLine}\"some value\";4\\,2",
                    $"a;b{NewLine}\"some value\";4\\,3",
                    "different", "value", "4,2 while actual 4,3"
                };
                yield return new object[]
                {
                    $"a;b{NewLine}\"some\\; value\";19",
                    $"a;b{NewLine}\"some; diff value\";19",
                    "different", "value", "\"some; value\" while actual \"some; diff value\""
                };
            }
        }

        [Theory]
        [MemberData(nameof(FailingBeEquivalentCases))]
        public void Compare_NotEqual_ShouldFailWithDifference(string expectedCsv, string actualCsv, params string[] expectedDifferences)
        {
            CompareShouldFailWithDescription(LoadCsv(expectedCsv), LoadCsv(actualCsv), configureOptions: null, expectedDifferences);
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

        private static void CompareShouldFailWithDescription(CsvTable expectedCsv, CsvTable actualCsv, Action<AssertCsvOptions> configureOptions, params string[] expectedDifferences)
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
            CsvTable expectedCsv = LoadCsv(expected, tag: "Expected", configureOptions: configureOptions);
            CsvTable actualCsv = LoadCsv(actual, tag: "Actual", configureOptions: configureOptions);

            EqualCsv(expectedCsv, actualCsv, configureOptions);
        }

        private static void EqualCsv(CsvTable expected, CsvTable actual, Action<AssertCsvOptions> configureOptions = null)
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
            TestCsv expected = TestCsv.Generate();

            // Act
            CsvTable actual = LoadCsv(expected, opt =>
            {
                opt.Separator = expected.Separator;
                opt.NewLine = expected.NewLine;
            });

            // Assert
            Assert.Equal(expected.ColumnCount, actual.ColumnCount);
            Assert.Equal(expected.HeaderNames, actual.HeaderNames);
            Assert.Equal(expected.RowCount, actual.Rows.Count);

            Assert.All(expected.Rows.Skip(1).Zip(actual.Rows), line =>
            {
                Assert.All(line.First.Zip(line.Second.Cells), cell =>
                {
                    EqualCellValue(cell.First, cell.Second.Value);
                });
            });
        }

        [Property]
        public void Load_WithRandomCsvWithoutHeader_Succeeds()
        {
            // Arrange
            TestCsv expected = TestCsv.Generate(opt => opt.Header = AssertCsvHeader.Missing);

            // Act
            CsvTable actual = LoadCsv(expected, opt =>
            {
                opt.Header = AssertCsvHeader.Missing;
                opt.Separator = expected.Separator;
                opt.NewLine = expected.NewLine;
            });

            // Assert
            Assert.Equal(expected.ColumnCount, actual.ColumnCount);
            Assert.Equal(expected.HeaderNames, actual.HeaderNames);

            Assert.Equal(expected.RowCount, actual.Rows.Count);
            Assert.All(expected.Rows.Zip(actual.Rows), line =>
            {
                Assert.All(line.First.Zip(line.Second.Cells), cell =>
                {
                    EqualCellValue(cell.First, cell.Second.Value);
                });
            });
        }

        private static void EqualCellValue(string expected, string actual)
        {
            Assert.Equal(expected.Replace("\\", string.Empty), actual);

        }

        public static IEnumerable<object[]> SucceedingCsvSizesCases
        {
            get
            {
                yield return new object[] { $"a;b;c{NewLine}1;2;3", 1, 3 };
                yield return new object[] { $"a;b;c;{NewLine}1;2;3;", 1, 4 };
                yield return new object[] { $"a;b;c{NewLine}1;2;3{NewLine}", 1, 3 };
                yield return new object[] { $"a;b{NewLine}1\\;2;3", 1, 2 };
                yield return new object[] { $"a;b{NewLine}\"1\\;2\";3", 1, 2 };
                yield return new object[] { $"a;b{NewLine}\"1;2\";3", 1, 2 };
                yield return new object[] { $"a;b{NewLine}\"1\\\"2\";3", 1, 2 };
                yield return new object[] { $"a;b{NewLine}\"1;\\\"2\";3", 1, 2 };
            }
        }

        [Theory]
        [MemberData(nameof(SucceedingCsvSizesCases))]
        public void Load_WithSpecialCases_Succeeds(string csv, int rows, int cols)
        {
            // Act
            CsvTable table = AssertCsv.Load(csv);

            // Assert
            Assert.Equal(rows, table.RowCount);
            Assert.Equal(cols, table.ColumnCount);
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
            CsvTable doc = LoadCsv(csv, opt => opt.Header = AssertCsvHeader.Present);

            // Assert
            Assert.Equal(csv.ColumnCount, doc.ColumnCount);
            Assert.Equal(0, doc.RowCount);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Load_WithBlankInput_FailsGenerally(string csv)
        {
            Assert.ThrowsAny<ArgumentException>(() => LoadCsv(csv, opt => opt.Header = AssertCsvHeader.Present));
            Assert.ThrowsAny<ArgumentException>(() => LoadCsv(csv, opt => opt.Header = AssertCsvHeader.Missing));
        }

        private void LoadShouldFailWithDescription(TestCsv csv, params string[] expectedFailures)
        {
            var exception = Assert.ThrowsAny<CsvException>(() => LoadCsv(csv));
            Assert.Contains(nameof(AssertCsv), exception.Message);
            Assert.All(expectedFailures, expectedFailure => Assert.Contains(expectedFailure, exception.Message));
        }

        private CsvTable LoadCsv(TestCsv csv, Action<AssertCsvOptions> configureOptions = null, string tag = "Input")
        {
            return LoadCsv(csv.ToString(), tag: tag, configureOptions: options =>
            {
                options.NewLine = csv.NewLine;
                options.Separator = csv.Separator;

                configureOptions?.Invoke(options);
            });
        }

        private CsvTable LoadCsv(string csv, Action<AssertCsvOptions> configureOptions = null, string tag = "Input")
        {
            _outputWriter.WriteLine("{0}: {1}", NewLine + tag, csv + NewLine);

            return configureOptions is null
                ? AssertCsv.Load(csv)
                : AssertCsv.Load(csv, configureOptions);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void IgnoreColumn_WithoutName_Fails(string columnName)
        {
            // Arrange
            var options = new AssertCsvOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.IgnoreColumn(columnName));
        }

        [Fact]
        public void IgnoreColumn_WithNegativeIndex_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.IgnoreColumn(Bogus.Random.Int(max: -1)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NewLine_WithoutValue_Fails(string newLine)
        {
            // Arrange
            var options = new AssertCsvOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.NewLine = newLine);
        }

        [Fact]
        public void Header_WithOutsideValue_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();
            var header = (AssertCsvHeader) Bogus.Random.Int(min: 10, max: 100);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Header = header);
        }

        [Fact]
        public void RowOrder_WithOutsideValue_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();
            var order = (AssertCsvOrder) Bogus.Random.Int(min: 10, max: 100);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.RowOrder = order);
        }

        [Fact]
        public void ColumnOrder_WithOutsideValue_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();
            var order = (AssertCsvOrder) Bogus.Random.Int(min: 10, max: 100);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.ColumnOrder = order);
        }

        [Fact]
        public void CultureInfo_WithoutValue_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.CultureInfo = null);
        }

        [Fact]
        public void MaxInputCharacters_WithNegativeValue_Fails()
        {
            // Arrange
            var options = new AssertCsvOptions();
            int max = Bogus.Random.Int(max: -1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MaxInputCharacters = max);
        }
    }
}
