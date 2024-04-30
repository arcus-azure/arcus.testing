using System;
using System.Linq;
using Arcus.Testing.Failure;
using static Arcus.Testing.CsvDifferenceKind;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents how the CSV table handles its header when comparing tables with <see cref="AssertCsv"/>.
    /// </summary>
    public enum CsvHeader
    {
        /// <summary>
        /// Indicate that the CSV table has an header present.
        /// </summary>
        Present,

        /// <summary>
        /// Indicate that the CSV table misses a header.
        /// </summary>
        Missing
    }

    /// <summary>
    /// Represents the ordering when comparing two CSV tables.
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
        private int _maxInputCharacters = ReportBuilder.DefaultMaxInputCharacters;
        private string _separator = ";", _newLine = Environment.NewLine;
        private CsvHeader _header = CsvHeader.Present;
        private AssertCsvOrder _order = AssertCsvOrder.Include;

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
        /// Gets or sets the new line character to be used when determining CSV lines in the loaded table, default: <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is empty.</exception>
        public string NewLine
        {
            get => _newLine;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Requires a non-empty CSV new line character to load the CSV table", nameof(value));
                }

                _newLine = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of header handling the loaded CSV table should have (default: <see cref="CsvHeader.Present"/>).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public CsvHeader Header
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
        /// Gets or sets the type of order which should be used when comparing CSV tables (default: <see cref="AssertCsvOrder.Include"/>).
        /// </summary>
        /// <remarks>
        ///     Only the rows can be configured to ignore their place in the table.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is outside the bounds of the enumeration.</exception>
        public AssertCsvOrder Order
        {
            get => _order;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Requires a CSV order value that is within the bounds of the enumeration");
                }

                _order = value;
            }
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
            CsvDifference diff = FindFirstDifference(expected, actual, options);

            if (diff != null)
            {
                throw new EqualAssertionException(
                    ReportBuilder.ForMethod(EqualMethodName, "expected and actual CSV contents do not match")
                                 .AppendLine(diff.ToString())
                                 .AppendDiff(expectedCsv, actualCsv, options.MaxInputCharacters)
                                 .ToString());
            }
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
            if (expected.HeaderNames.Length != actual.HeaderNames.Length)
            {
                return new(DifferentColumnLength, expected.HeaderNames.Length, actual.HeaderNames.Length);
            }

            if (expected.LineCount != actual.LineCount)
            {
                return new(DifferentLineLength, expected.LineCount, actual.LineCount);
            }

            for (var i = 0; i < expected.HeaderNames.Length; i++)
            {
                string expectedHeader = expected.HeaderNames[i];
                string actualHeader = actual.HeaderNames[i];
                if (expectedHeader != actualHeader)
                {
                    return new(ActualMissingColumn, expectedHeader, actualHeader, lineNumber: 0);
                }
            }

            CsvDifference diff = CompareLines(expected.Lines, actual.Lines, options);
            if (diff != null)
            {
                return diff;
            }

            return null;
        }

        private static CsvDifference CompareLines(CsvLine[] expected, CsvLine[] actual, AssertCsvOptions options)
        {
            bool shouldIgnoreOrder = options.Order is AssertCsvOrder.Ignore && expected.Length > 1;
            if (shouldIgnoreOrder)
            {
                expected = expected.OrderBy(l => l).ToArray();
                actual = actual.OrderBy(l => l).ToArray();
            }

            for (var line = 0; line < expected.Length; line++)
            {
                CsvLine expectedLine = expected[line];
                CsvLine actualLine = actual[line];

                for (var col = 0; col < expectedLine.Cells.Length; col++)
                {
                    CsvCell expectedCell = expectedLine.Cells[col];
                    CsvCell actualCell = actualLine.Cells[col];
                    if (!expectedCell.Equals(actualCell))
                    {
                        return shouldIgnoreOrder
                            ? new(ActualMissingLine, expectedLine, actualLine)
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
        private readonly int _lineNumber;

        internal CsvDifference(CsvDifferenceKind kind, CsvLine expected, CsvLine actual)
            : this(kind, expected.ToString(), actual.ToString(), expected.LineNumber)
        {
        }

        internal CsvDifference(CsvDifferenceKind kind, CsvCell expected, CsvCell actual)
            : this(kind, expected.Value, actual.Value, expected.LineNumber)
        {
            _column = expected.ColumnHeader;
        }

        internal CsvDifference(CsvDifferenceKind kind, int expected, int actual)
            : this(kind, expected.ToString(), actual.ToString(), lineNumber: 0)
        {
        }

        internal CsvDifference(CsvDifferenceKind kind, string expected, string actual, int lineNumber)
        {
            _kind = kind;
            _expected = expected;
            _actual = actual;
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _kind switch
            {
                ActualMissingColumn => $"actual CSV is missing a column: {_expected}",
                ActualMissingLine => $"actual CSV does not contain a line: {_expected} which was found in expected CSV at line number {_lineNumber} (index-based, excluding header)",
                ActualOtherValue => $"actual CSV cell has a different value at line number {_lineNumber} (index-based, excluding header), expected {_expected} while actual {_actual} for column {_column}",
                DifferentColumnLength => $"actual CSV has {_actual} columns instead of {_expected}",
                DifferentLineLength => $"actual CSV has {_actual} lines instead of {_expected}",
                _ => throw new ArgumentOutOfRangeException(nameof(_kind), _kind, "Unknown CSV difference kind type")
            };
        }
    }

    /// <summary>
    /// Represents the type of <see cref="CsvDifference"/>.
    /// </summary>
    internal enum CsvDifferenceKind
    {
        ActualMissingColumn,
        ActualMissingLine,
        ActualOtherValue,
        DifferentColumnLength,
        DifferentLineLength
    }

    /// <summary>
    /// Represents a CSV tabular data structure with named columns.
    /// </summary>
    public sealed class CsvTable
    {
        private readonly string _originalCsv;
        private const string LoadMethodName = $"{nameof(AssertCsv)}.{nameof(Load)}";

        private CsvTable(string[] headerNames, CsvLine[] lines, string originalCsv)
        {
            _originalCsv = originalCsv;

            HeaderNames = headerNames;
            LineCount = lines.Length;
            ColumnCount = headerNames.Length;
            Lines = lines;
        }

        /// <summary>
        /// Gets the names of the headers of the first line in the table.
        /// </summary>
        public string[] HeaderNames { get; }

        /// <summary>
        /// Gets the amount of lines the table has.
        /// </summary>
        public int LineCount { get; }

        /// <summary>
        /// Gets the amount of columns the table has.
        /// </summary>
        public int ColumnCount { get; }

        /// <summary>
        /// Gets the lines of the table.
        /// </summary>
        public CsvLine[] Lines { get; }

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
                   .Select(line => line.Split(options.Separator))
                   .ToArray();

            EnsureAllLinesSameLength(csv, rawLines);

            string[] headerNames;
            if (options.Header is CsvHeader.Present)
            {
                headerNames = rawLines[0];
                rawLines = rawLines.Skip(1).ToArray();
            }
            else
            {
                headerNames = Enumerable.Range(0, rawLines[0].Length).Select(i => $"Col #{i}").ToArray();
            }

            CsvLine[] lines = rawLines.Select((rawLine, lineNumber) =>
            {
                CsvCell[] cells = rawLine.Select((cellValue, columnNumber) =>
                {
                    string headerName = headerNames[columnNumber];
                    return new CsvCell(headerName, columnNumber, lineNumber, cellValue);
                }).ToArray();

                return new CsvLine(cells, lineNumber, options);
            }).ToArray();

            return new CsvTable(headerNames, lines, csv);
        }

        private static void EnsureAllLinesSameLength(string csv, string[][] rawLines)
        {
            var linesWithDiffLength =
                rawLines.GroupBy(line => line.Length)
                        .ToArray();

            if (linesWithDiffLength.Length > 1)
            {
                string description =
                    linesWithDiffLength.OrderBy(line => line.Key)
                                       .Select(line => $"\t - {line.Count()} line(s) with {line.Key} columns")
                                       .Aggregate((x, y) => x + Environment.NewLine + y);

                throw new CsvException(
                    ReportBuilder.ForMethod(LoadMethodName, "cannot correctly load the CSV contents as not all lines in the CSV table has the same amount of columns:")
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
    /// Represents a single line within the <see cref="CsvTable"/>.
    /// </summary>
    public sealed class CsvLine : IComparable<CsvLine>
    {
        private readonly AssertCsvOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvLine" /> class.
        /// </summary>
        internal CsvLine(CsvCell[] cells, int lineNumber, AssertCsvOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Represents all the cells of a single line within a <see cref="CsvTable"/>.
        /// </summary>
        public CsvCell[] Cells { get; }

        /// <summary>
        /// Gets the index where the line resides within the <see cref="CsvTable"/>.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description> This instance precedes <paramref name="other" /> in the sort order.</description></item><item><term> Zero</term><description> This instance occurs in the same position in the sort order as <paramref name="other" />.</description></item><item><term> Greater than zero</term><description> This instance follows <paramref name="other" /> in the sort order.</description></item></list></returns>
        public int CompareTo(CsvLine other)
        {
            return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
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
    /// Represents a single cell of a <see cref="CsvLine"/> within a <see cref="CsvTable"/>.
    /// </summary>
    public sealed class CsvCell : IEquatable<CsvCell>
    {
        internal CsvCell(string headerName, int columnNumber, int lineNumber, string value)
        {
            ColumnHeader = headerName;
            ColumnNumber = columnNumber;
            LineNumber = lineNumber;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the header of current column.
        /// </summary>
        public string ColumnHeader { get; }

        /// <summary>
        /// Gets the value of the current cell's location.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the number of the line of this current cell within the table.
        /// </summary>
        public int LineNumber { get; }

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

            if (float.TryParse(Value, out float expectedValue)
                && float.TryParse(other.Value, out float actualValue))
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
