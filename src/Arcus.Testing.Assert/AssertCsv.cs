﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Arcus.Testing.Failure;
using static Arcus.Testing.CsvDifferenceKind;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how the CSV table handles its header when comparing tables with <see cref="AssertCsv"/>.
    /// </summary>
    public enum AssertCsvHeader
    {
        /// <summary>
        /// Indicate that the CSV table has an header present.
        /// </summary>
        Present = 0,

        /// <summary>
        /// Indicate that the CSV table misses a header.
        /// </summary>
        Missing
    }

    /// <summary>
    /// Represents the ordering when comparing two CSV tables with <see cref="AssertCsv"/>.
    /// </summary>
    public enum AssertCsvOrder
    {
        /// <summary>
        /// Take the order of rows into account when comparing tables (default).
        /// </summary>
        Include = 0,

        /// <summary>
        /// Ignore the order of rows when comparing tables.
        /// </summary>
        Ignore
    }

    /// <summary>
    /// Represents the available options when asserting on different CSV tables in <see cref="AssertCsv"/>.
    /// </summary>
    public class AssertCsvOptions
    {
        private readonly Collection<string> _ignoredColumns = new();
        private int _maxInputCharacters = ReportBuilder.DefaultMaxInputCharacters;
        private string _separator = ";", _newRow = Environment.NewLine;
        private AssertCsvHeader _header = AssertCsvHeader.Present;
        private AssertCsvOrder _rowOrder = AssertCsvOrder.Include, _columnOrder = AssertCsvOrder.Include;
        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        /// <summary>
        /// Adds a column which will get ignored when comparing CSV tables.
        /// </summary>
        /// <param name="headerName">The name of the column that should be ignored.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> is blank.</exception>
        public AssertCsvOptions IgnoreColumn(string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException($"Requires a non-blank '{nameof(headerName)}' when adding an ignored column of a CSV table", nameof(headerName));
            }

            _ignoredColumns.Add(headerName);
            return this;
        }

        /// <summary>
        /// Gets the header names of the columns that should be ignored when comparing CSV tables.
        /// </summary>
        internal IReadOnlyCollection<string> IgnoredColumns => _ignoredColumns;

        /// <summary>
        /// Gets or sets the separator character to be used when determining CSV columns in the loaded table, default semicolon: ';'.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string Separator
        {
            get => _separator;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Requires a non-blank CSV separator character to load the CSV table", nameof(value));
                }

                _separator = value;
            }
        }

        /// <summary>
        /// Gets or sets the new row character to be used when determining CSV rows in the loaded table, default: <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is empty.</exception>
        public string NewLine
        {
            get => _newRow;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Requires a non-empty CSV new row character to load the CSV table", nameof(value));
                }

                _newRow = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of header handling the loaded CSV table should have (default: <see cref="AssertCsvHeader.Present"/>).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertCsvHeader Header
        {
            get => _header;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Requires a CSV header value that is within the bounds of the enumeration");
                }

                _header = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of row order which should be used when comparing CSV tables (default: <see cref="AssertCsvOrder.Include"/>).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertCsvOrder RowOrder
        {
            get => _rowOrder;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Requires a CSV row order value that is within the bounds of the enumeration");
                }

                _rowOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of column order which should be used when comparing CSV tables (default: <see cref="AssertCsvOrder.Include"/>).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertCsvOrder ColumnOrder
        {
            get => _columnOrder;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Requires a CSV column order value that is within the bounds of the enumeration");
                }

                _columnOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets the specific culture of the loaded CSV tables - this is especially useful when comparing floating numbers.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is <c>null</c>.</exception>
        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set => _cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the maximum characters of the expected and actual inputs should be written to the test output.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is lower than zero.</exception>
        public int MaxInputCharacters
        {
            get => _maxInputCharacters;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum input characters cannot be lower than zero");
                }

                _maxInputCharacters = value;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Options: {Environment.NewLine}" +
                   $"\t- ignored columns: [{string.Join($"{Separator} ", _ignoredColumns)}]{Environment.NewLine}" +
                   $"\t- column order: {ColumnOrder}{Environment.NewLine}" +
                   $"\t- row order: {RowOrder}";
        }
    }

    /// <summary>
    /// Represents assertion-like functionality related to comparing CSV tables.
    /// </summary>
    public static class AssertCsv
    {
        private const string EqualMethodName = $"{nameof(AssertCsv)}.{nameof(Equal)}";

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedCsv"/> is the same as the <paramref name="actualCsv"/>.
        /// </summary>
        /// <param name="expectedCsv">The raw contents of the expected CSV table.</param>
        /// <param name="actualCsv">The raw contents of the actual Csv table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedCsv"/> or the <paramref name="actualCsv"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">
        ///     Thrown when the <paramref name="expectedCsv"/> or the <paramref name="actualCsv"/> could not be successfully loaded into a structured Csv table.
        /// </exception>
        public static void Equal(string expectedCsv, string actualCsv)
        {
            Equal(expectedCsv ?? throw new ArgumentNullException(nameof(expectedCsv)),
                  actualCsv ?? throw new ArgumentNullException(nameof(actualCsv)),
                  configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expectedCsv"/> is the same as the <paramref name="actualCsv"/>.
        /// </summary>
        /// <param name="expectedCsv">The raw contents of the expected CSV table.</param>
        /// <param name="actualCsv">The raw contents of the actual Csv table.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expectedCsv"/> or the <paramref name="actualCsv"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">
        ///     Thrown when the <paramref name="expectedCsv"/> or the <paramref name="actualCsv"/> could not be successfully loaded into a structured Csv table.
        /// </exception>
        public static void Equal(string expectedCsv, string actualCsv, Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            var expected = CsvTable.Load(expectedCsv ?? throw new ArgumentNullException(nameof(expectedCsv)), options);
            var actual = CsvTable.Load(actualCsv ?? throw new ArgumentNullException(nameof(actualCsv)), options);
            
            Equal(expected, actual, configureOptions);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expected"/> is the same as the <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">The expected CSV table.</param>
        /// <param name="actual">The actual CSV table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">
        ///     Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> could not be successfully loaded into a structured Csv table.
        /// </exception>
        public static void Equal(CsvTable expected, CsvTable actual)
        {
            Equal(expected ?? throw new ArgumentNullException(nameof(expected)), 
                  actual ?? throw new ArgumentNullException(nameof(actual)), 
                  configureOptions: null);
        }

        /// <summary>
        /// Verifies if the given raw <paramref name="expected"/> is the same as the <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">The expected CSV table.</param>
        /// <param name="actual">The actual CSV table.</param>
        /// <param name="configureOptions">The function to configure additional comparison options.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">
        ///     Thrown when the <paramref name="expected"/> or the <paramref name="actual"/> could not be successfully loaded into a structured Csv table.
        /// </exception>
        public static void Equal(CsvTable expected, CsvTable actual, Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            CsvDifference diff = FindFirstDifference(
                expected ?? throw new ArgumentNullException(nameof(expected)),
                actual ?? throw new ArgumentNullException(nameof(actual)),
                options);

            if (diff != null)
            {
                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "expected and actual CSV contents do not match")
                                 .AppendLine(diff.ToString())
                                 .AppendLine()
                                 .AppendLine(options.ToString())
                                 .AppendDiff(expected.ToString(), actual.ToString(), options.MaxInputCharacters)
                                 .ToString());
            }
        }

        /// <summary>
        /// Loads the given raw <paramref name="csv"/> contents into a structured CSV table.
        /// </summary>
        /// <param name="csv">The raw CSV input contents.</param>
        /// <returns>The loaded CSV table.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="csv"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">Thrown when the <paramref name="csv"/> could not be successfully loaded into a structured CSV table.</exception>
        public static CsvTable Load(string csv)
        {
            return Load(csv ?? throw new ArgumentNullException(nameof(csv)), configureOptions: null);
        }

        /// <summary>
        /// Loads the given raw <paramref name="csv"/> contents into a structured CSV table.
        /// </summary>
        /// <param name="csv">The raw CSV input contents.</param>
        /// <param name="configureOptions">The function to configure the options to control the behavior when loading the <paramref name="csv"/> contents.</param>
        /// <returns>The loaded CSV table.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="csv"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">Thrown when the <paramref name="csv"/> could not be successfully loaded into a structured CSV table.</exception>
        public static CsvTable Load(string csv, Action<AssertCsvOptions> configureOptions)
        {
            var options = new AssertCsvOptions();
            configureOptions?.Invoke(options);

            return CsvTable.Load(csv ?? throw new ArgumentNullException(nameof(csv)), options);
        }

        private static CsvDifference FindFirstDifference(CsvTable expected, CsvTable actual, AssertCsvOptions options)
        {
            EnsureOnlyIgnoreColumnsOnPresentHeaders(expected, actual, options);
            EnsureOnlyIgnoreColumnsOnUniqueHeaders(expected, options);

            if (expected.HeaderNames.Count != actual.HeaderNames.Count)
            {
                return new(DifferentColumnLength, expected.HeaderNames.Count, actual.HeaderNames.Count);
            }

            if (expected.RowCount != actual.RowCount)
            {
                return new(DifferentRowLength, expected.RowCount, actual.RowCount);
            }

            return CompareHeaders(expected, actual, options) ?? CompareRows(expected, actual, options);
        }

        private static void EnsureOnlyIgnoreColumnsOnPresentHeaders(CsvTable expected, CsvTable actual, AssertCsvOptions options)
        {
            bool missingHeaders = expected.Header is AssertCsvHeader.Missing || actual.Header is AssertCsvHeader.Missing;
            if (missingHeaders && options.ColumnOrder is AssertCsvOrder.Ignore)
            {
                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "cannot compare expected and actual CSV contents")
                                 .AppendLine($"order of columns can only be ignored when the header names are present in the expected and actual CSV tables, " +
                                             $"please provide such headers in the contents, or remove the 'options.{nameof(AssertCsvOptions.ColumnOrder)}={AssertCsvOrder.Ignore}'")
                                 .ToString());
            }

            if (missingHeaders && options.IgnoredColumns.Count > 0)
            {
                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "cannot compare expected and actual CSV contents")
                                 .AppendLine($"specific column(s) can only be ignored when the header names are present in the expected and actual CSV tables, " +
                                             $"please provide such headers in the contents, or remove the 'options.{nameof(AssertCsvOptions.IgnoreColumn)}' call(s)")
                                 .ToString());
            }
        }

        private static void EnsureOnlyIgnoreColumnsOnUniqueHeaders(CsvTable expected, AssertCsvOptions options)
        {
            var duplicateHeaderNames =
                expected.HeaderNames.Where(n => !options.IgnoredColumns.Contains(n))
                                    .GroupBy(n => n)
                                    .Where(n => n.Count() > 1)
                                    .ToArray();

            if (duplicateHeaderNames.Length > 0 && options.ColumnOrder is AssertCsvOrder.Ignore)
            {
                var description = string.Join(", ", duplicateHeaderNames.Select(h => h.Key));

                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "cannot compare expected and actual CSV contents")
                                 .AppendLine($"columns can only be ignored when the header names are unique, but got duplicates: [{description}], " +
                                             $"please either remove the 'options.{nameof(AssertCsvOptions.ColumnOrder)}={AssertCsvOrder.Ignore}' or ignore these columns with 'options.{nameof(AssertCsvOptions.IgnoreColumn)}'")
                                 .ToString());
            }
        }

        private static CsvDifference CompareHeaders(CsvTable expected, CsvTable actual, AssertCsvOptions options)
        {
            if (expected.Header != actual.Header)
            {
                return new(DifferentHeaderConfig, expected.Header.ToString(), actual.Header.ToString(), rowNumber: 0);
            }

            IReadOnlyCollection<string> 
                expectedHeaders = expected.HeaderNames,
                actualHeaders = actual.HeaderNames;
            
            if (options.ColumnOrder is AssertCsvOrder.Ignore)
            {
                expectedHeaders = expectedHeaders.Order().ToArray();
                actualHeaders = actualHeaders.Order().ToArray();
            }

            for (var i = 0; i < expectedHeaders.Count; i++)
            {
                string expectedHeader = expectedHeaders.ElementAt(i),
                       actualHeader = actualHeaders.ElementAt(i);
                
                if (expectedHeader != actualHeader)
                {
                    return new(ActualMissingColumn, expectedHeader, actualHeader, rowNumber: 0);
                }
            }

            return null;
        }

        private static CsvDifference CompareRows(CsvTable expectedCsv, CsvTable actualCsv, AssertCsvOptions options)
        {
            IReadOnlyCollection<CsvRow> 
                expectedRows = expectedCsv.Rows,
                actualRows = actualCsv.Rows;
            
            if (options.ColumnOrder is AssertCsvOrder.Ignore)
            {
                expectedRows = CsvRow.WithOrderedCells(expectedRows);
                actualRows = CsvRow.WithOrderedCells(actualRows);
            }

            bool shouldIgnoreOrder = options.RowOrder is AssertCsvOrder.Ignore && expectedRows.Count > 1;
            if (shouldIgnoreOrder)
            {
                expectedRows = CsvRow.WithOrderedRows(expectedRows, options);
                actualRows = CsvRow.WithOrderedRows(actualRows, options);
            }

            for (var row = 0; row < expectedRows.Count; row++)
            {
                CsvRow expectedRow = expectedRows.ElementAt(row),
                       actualRow = actualRows.ElementAt(row);
                
                IReadOnlyCollection<CsvCell> 
                    expectedCells = expectedRow.Cells,
                    actualCells = actualRow.Cells;

                for (var col = 0; col < expectedCells.Count; col++)
                {
                    CsvCell expectedCell = expectedCells.ElementAt(col),
                            actualCell = actualCells.ElementAt(col);

                    if (options.IgnoredColumns.Contains(expectedCell.HeaderName))
                    {
                        continue;
                    }

                    if (!expectedCell.Equals(actualCell))
                    {
                        return shouldIgnoreOrder
                            ? new(ActualMissingRow, expectedRow, actualRow)
                            : new(ActualOtherValue,  expectedCell, actualCell);
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Represents the single found difference between two JSON contents.
    /// </summary>
    internal class CsvDifference
    {
        private readonly CsvDifferenceKind _kind;
        private readonly string _expected, _actual, _column;
        private readonly int _rowNumber;

        internal CsvDifference(CsvDifferenceKind kind, CsvRow expected, CsvRow actual)
            : this(kind, expected.ToString(), actual.ToString(), expected.RowNumber)
        {
        }

        internal CsvDifference(CsvDifferenceKind kind, CsvCell expected, CsvCell actual)
            : this(kind, expected.Value, actual.Value, expected.RowNumber)
        {
            _column = expected.HeaderName;
        }

        internal CsvDifference(CsvDifferenceKind kind, int expected, int actual)
            : this(kind, expected.ToString(), actual.ToString(), rowNumber: 0)
        {
        }

        internal CsvDifference(CsvDifferenceKind kind, string expected, string actual, int rowNumber)
        {
            _kind = kind;
            _expected = expected ?? throw new ArgumentNullException(nameof(expected));
            _actual = actual ?? throw new ArgumentNullException(nameof(actual));
            _rowNumber = rowNumber;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return DetermineErrorMessage(_kind);
        }

        private string DetermineErrorMessage(CsvDifferenceKind kind)
        {
            return kind switch
            {
                ActualMissingColumn => $"actual CSV is missing a column: {_expected}",
                ActualMissingRow => $"actual CSV does not contain a row: {_expected} which was found in expected CSV at row number {_rowNumber} (index-based, excluding header)",
                ActualOtherValue => $"actual CSV cell has a different value at row number {_rowNumber} (index-based, excluding header), expected {_expected} while actual {_actual} for column {_column}",
                DifferentColumnLength => $"actual CSV has {_actual} columns instead of {_expected}",
                DifferentRowLength => $"actual CSV has {_actual} rows instead of {_expected}",
                DifferentHeaderConfig => $"expected CSV is configured with '{_expected}' CSV header while actual is configured with '{_actual}' CSV header",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CSV difference kind type")
            };
        }
    }

    /// <summary>
    /// Represents the type of <see cref="CsvDifference"/>.
    /// </summary>
    internal enum CsvDifferenceKind
    {
        ActualMissingColumn,
        ActualMissingRow,
        ActualOtherValue,
        DifferentHeaderConfig,
        DifferentColumnLength,
        DifferentRowLength
    }

    /// <summary>
    /// Represents a CSV tabular data structure with named columns.
    /// </summary>
    public sealed class CsvTable
    {
        private readonly string _originalCsv;
        private readonly AssertCsvOptions _options;
        private const string LoadMethodName = $"{nameof(AssertCsv)}.{nameof(Load)}";

        private CsvTable(string[] headerNames, CsvRow[] rows, string originalCsv, AssertCsvOptions options)
        {
            _originalCsv = originalCsv ?? throw new ArgumentNullException(nameof(originalCsv));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            HeaderNames = headerNames ?? throw new ArgumentNullException(nameof(headerNames));
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            RowCount = rows.Length;
            ColumnCount = headerNames.Length;
        }

        internal AssertCsvHeader Header => _options.Header;

        /// <summary>
        /// Gets the names of the headers of the first row in the table.
        /// </summary>
        public IReadOnlyCollection<string> HeaderNames { get; }

        /// <summary>
        /// Gets the amount of rows the table has.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// Gets the amount of columns the table has.
        /// </summary>
        public int ColumnCount { get; }

        /// <summary>
        /// Gets the rows of the table.
        /// </summary>
        public IReadOnlyCollection<CsvRow> Rows { get; }

        /// <summary>
        /// Loads the raw <paramref name="csv"/> to a validly parsed <see cref="CsvTable"/>.
        /// </summary>
        /// <param name="csv">The raw contents that should represent the table.</param>
        /// <param name="options">The user-defined options that control certain behavior of how the table should be loaded.</param>
        /// <returns>The loaded CSV table.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="csv"/> or the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="CsvException">Thrown when the raw <paramref name="csv"/> contents does not represent a valid CSV structure.</exception>
        internal static CsvTable Load(string csv, AssertCsvOptions options)
        {
            ArgumentNullException.ThrowIfNull(csv);
            ArgumentNullException.ThrowIfNull(options);

            string[][] rawLines = 
                csv.Split(options.NewLine)
                   .Select(row => row.Split(options.Separator))
                   .ToArray();

            EnsureAllRowsSameLength(csv, rawLines);

            string[] headerNames;
            if (options.Header is AssertCsvHeader.Present)
            {
                headerNames = rawLines[0];
                rawLines = rawLines.Skip(1).ToArray();
            }
            else
            {
                headerNames = Enumerable.Range(0, rawLines[0].Length).Select(i => $"Col #{i}").ToArray();
            }

            CsvRow[] rows = rawLines.Select((rawRow, rowNumber) =>
            {
                CsvCell[] cells = rawRow.Select((cellValue, columnNumber) =>
                {
                    string headerName = headerNames[columnNumber];
                    return new CsvCell(headerName, columnNumber, rowNumber, cellValue, options.CultureInfo);
                }).ToArray();

                return new CsvRow(cells, rowNumber, options);
            }).ToArray();

            return new CsvTable(headerNames, rows, csv, options);
        }

        private static void EnsureAllRowsSameLength(string csv, string[][] rawRows)
        {
            var rowsWithDiffLength =
                rawRows.GroupBy(row => row.Length)
                        .ToArray();

            if (rowsWithDiffLength.Length > 1)
            {
                string description =
                    rowsWithDiffLength.OrderBy(row => row.Key)
                                      .Select(row => $"\t - {row.Count()} row(s) with {row.Key} columns")
                                      .Aggregate((x, y) => x + Environment.NewLine + y);

                throw new CsvException(
                    ReportBuilder.ForMethod(LoadMethodName, "cannot correctly load the CSV contents as not all rows in the CSV table has the same amount of columns")
                                 .AppendLine(description)
                                 .AppendInput(csv)
                                 .ToString());
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _originalCsv;
        }
    }

    /// <summary>
    /// Represents a single row within the <see cref="CsvTable"/>.
    /// </summary>
    public sealed class CsvRow
    {
        private readonly AssertCsvOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvRow" /> class.
        /// </summary>
        internal CsvRow(CsvCell[] cells, int rowNumber, AssertCsvOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            RowNumber = rowNumber;
        }

        /// <summary>
        /// Represents all the cells of a single row within a <see cref="CsvTable"/>.
        /// </summary>
        public IReadOnlyCollection<CsvCell> Cells { get; private set; }

        /// <summary>
        /// Gets the index where the row resides within the <see cref="CsvTable"/>.
        /// </summary>
        public int RowNumber { get; }

        /// <summary>
        /// Order the cells of each row in the passed <paramref name="rows"/>, a.k.a. horizontal ordering.
        /// </summary>
        internal static CsvRow[] WithOrderedCells(IReadOnlyCollection<CsvRow> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            return rows.Select(row =>
            {
                row.Cells = row.Cells.OrderBy(c => c.HeaderName, StringComparer.InvariantCulture).ToArray();
                return row;
            }).ToArray();
        }

        /// <summary>
        /// Order the rows by the current configured <paramref name="options"/>, a.k.a. vertical ordering.
        /// </summary>
        internal static CsvRow[] WithOrderedRows(IReadOnlyCollection<CsvRow> rows, AssertCsvOptions options)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(options);

            return rows.OrderBy(r =>
            {
                string[] line = r.Cells.Where(c => !options.IgnoredColumns.Contains(c.HeaderName)).Select(c => c.Value).ToArray();
                return string.Join(options.Separator, line);
            }).ToArray();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Join(_options.Separator, Cells.Select(c => c.Value));
        }
    }

    /// <summary>
    /// Represents a single cell of a <see cref="CsvRow"/> within a <see cref="CsvTable"/>.
    /// </summary>
    public sealed class CsvCell : IEquatable<CsvCell>
    {
        private readonly CultureInfo _culture;

        internal CsvCell(string headerName, int columnNumber, int rowNumber, string value, CultureInfo culture)
        {
            _culture = culture;

            HeaderName = headerName;
            ColumnNumber = columnNumber;
            RowNumber = rowNumber;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the header of current column.
        /// </summary>
        public string HeaderName { get; }

        /// <summary>
        /// Gets the value of the current cell's location.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the number of the row of this current cell within the table.
        /// </summary>
        public int RowNumber { get; }

        /// <summary>
        /// Gets the number of the column of this current cell within the table.
        /// </summary>
        public int ColumnNumber { get; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
        public bool Equals(CsvCell other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (float.TryParse(Value, _culture, out float expectedValue)
                && float.TryParse(other.Value, _culture, out float actualValue))
            {
                if (!expectedValue.Equals(actualValue))
                {
                    return false;
                }
            }
            else if (Value != other.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is CsvCell other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
