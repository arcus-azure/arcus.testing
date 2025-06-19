using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Core.Assert_.Fixture
{
    /// <summary>
    /// Represents additional options to control how the generated <see cref="TestCsv"/> will look like.
    /// </summary>
    public class TestCsvOptions : AssertCsvOptions
    {
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Gets or sets the amount of rows the generated CSV table should have (ignoring the header).
        /// </summary>
        public int RowCount { get; set; } = Bogus.Random.Int(1, 10);

        /// <summary>
        /// Gets or sets the amount of columns the generated CSV table should have.
        /// </summary>
        public int ColumnCount { get; set; } = Bogus.Random.Int(1, 10);
    }

    /// <summary>
    /// Represents a test fixture that generates random CSV documents.
    /// </summary>
    public class TestCsv
    {
        private readonly TestCsvOptions _options;
        private List<List<string>> _columns;
        private readonly List<List<string>> _invalidRows = new();
        private static readonly Faker Bogus = new();
        private bool _shouldShuffleRows;

        private TestCsv(IEnumerable<string[]> columns, string[] headerNames, TestCsvOptions options)
        {
            _options = options;
            _columns = columns.Select(col => col.ToList()).ToList();

            HeaderNames = headerNames;
            ColumnCount = options.ColumnCount;
            RowCount = options.RowCount;

            IgnoredIndex = Bogus.PickRandom(Enumerable.Range(0, ColumnCount).ToArray());
        }

        /// <summary>
        /// Gets the names of the columns in the generated CSV table.
        /// </summary>
        public string[] HeaderNames { get; }

        /// <summary>
        /// Gets the collected rows of the CSV table (excluding the header).
        /// </summary>
        public string[][] Rows => Transpose(_columns).Select(col => col.ToArray()).ToArray();

        /// <summary>
        /// Gets the amount of columns of the generated CSV table
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Gets the amount of rows of the generated CSV table (excluding the header).
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// Gets the separator used for this generated CSV table.
        /// </summary>
        public char Separator => _options.Separator;

        /// <summary>
        /// Gets the new-row character used for this generated CSV table.
        /// </summary>
        public string NewLine => _options.NewLine;

        /// <summary>
        /// Gets the index of a column that should be ignored during the assertion.
        /// </summary>
        public int IgnoredIndex { get; }

        /// <summary>
        /// Generate a new <see cref="TestCsv"/> model.
        /// </summary>
        public static TestCsv Generate(Action<TestCsvOptions> configureOptions = null)
        {
            var options = new TestCsvOptions
            {
                Separator = Bogus.PickRandom(',', ';', '%'),
                NewLine = Bogus.PickRandom(Environment.NewLine, "\n")
            };
            configureOptions?.Invoke(options);

            IList<string[]> columns = Bogus.Make(options.ColumnCount, () =>
            {
                string[] col = GenerateColumn(options.RowCount);
                if (options.Header is AssertCsvHeader.Present)
                {
                    return col.Prepend(CreateColumnName()).ToArray();
                }

                return col.ToArray();
            });

            string[] headerNames =
                options.Header is AssertCsvHeader.Present
                    ? columns.Select(col => col[0]).ToArray()
                    : columns.Select((_, index) => $"Col #{index}").ToArray();

            return new TestCsv(columns, headerNames, options);
        }

        /// <summary>
        /// Adds a new random column to the CSV table.
        /// </summary>
        public string AddColumn(string headerName = null)
        {
            string columnName = headerName ?? CreateColumnName();
            List<string> newColumn = GenerateColumn(RowCount).Prepend(columnName).ToList();
            _columns.Insert(Bogus.Random.Int(0, _columns.Count - 1), newColumn);
            ColumnCount++;

            return columnName;
        }

        private static string CreateColumnName()
        {
            return Bogus.Database.Column() + Bogus.Random.Guid().ToString()[..7];
        }

        /// <summary>
        /// Adds a new row to the CSV table.
        /// </summary>
        public void AddRow()
        {
            Assert.All(_columns, col => col.Add(GenValueFunc()()));
            RowCount++;
        }

        /// <summary>
        /// Adds a new row to the CSV table that does not corresponds with the expected column count.
        /// </summary>
        public void AddInvalidRow()
        {
            _invalidRows.Add(Bogus.Make(Bogus.Random.Int(11, 20), GenValue).ToList());
        }

        /// <summary>
        /// Change a randomly picked column name in the CSV table.
        /// </summary>
        public string ChangeColumnValue()
        {
            List<string> col = Bogus.PickRandom(_columns);
            string original = col[0];
            col[0] = "changed-" + Bogus.Database.Column();

            return original;
        }

        /// <summary>
        /// Gets the index of a random cell within the CSV table.
        /// </summary>
        public (int row, int col) GetRandomCellIndex()
        {
            int col = Bogus.Random.Int(0, _columns.Count - 1);
            int row = Bogus.Random.Int(1, _columns[0].Count - 1);

            return (row, col);
        }

        /// <summary>
        /// Change the cell value at a certain location.
        /// </summary>
        public void ChangeCellValue(int row, int col, string value)
        {
            _columns[col][row] = value;
        }

        /// <summary>
        /// Change a randomly picked cell value in the CSV table.
        /// </summary>
        public (string headerName, string changedValue) ChangeCellValue()
        {
            List<string> col = Bogus.PickRandom(_columns);
            int index = Bogus.Random.Int(1, col.Count - 1);

            string changedValue = GenValue();
            col[index] = "diff-" + changedValue;

            return (col[0], changedValue);
        }

        /// <summary>
        /// Removes the headers from the generated CSV.
        /// </summary>
        public void RemoveHeaders()
        {
            _columns = _columns.Select(c => c.Skip(1).ToList()).ToList();
            _options.Header = AssertCsvHeader.Missing;
        }

        /// <summary>
        /// Shuffle the CSV table's rows.
        /// </summary>
        public void ShuffleRows()
        {
            _shouldShuffleRows = true;
        }

        /// <summary>
        /// Shuffle the CSV table's columns.
        /// </summary>
        public void ShuffleColumns()
        {
            _columns = Bogus.Random.Shuffle(_columns).ToList();
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public TestCsv Copy()
        {
            return new TestCsv(_columns.Select(col => col.ToArray()), HeaderNames, _options);
        }

        private static string[] GenerateColumn(int rowCount)
        {
            Func<string> genValue = GenValueFunc();
            return Bogus.Make(rowCount, genValue).ToArray();
        }

        private static string GenValue()
        {
            return GenValueFunc()();
        }

        private static Func<string> GenValueFunc()
        {
            CultureInfo cultureWithComma = new("nl-NL");
            CultureInfo cultureWithDot = new("en-US");

            return Bogus.PickRandom(
                () => Bogus.Lorem.Word(),
                () => $"\"{RandomlyInsert(Bogus.Lorem.Sentence(), ",", ";", "%", "\\\"")}\"",
                () => Bogus.Random.Int().ToString(),
                () => Bogus.Random.Float().ToString(cultureWithComma).Replace(",", "\\,"),
                () => Bogus.Random.Float().ToString(cultureWithDot),
                () => Bogus.Date.RecentOffset().ToString());
        }

        private static string RandomlyInsert(string input, params string[] values)
        {
            return values.Aggregate(input, (acc, value) =>
            {
                int index = Bogus.Random.Int(0, acc.Length - 1);
                return acc.Insert(index, value);
            });
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            List<List<string>> rows = Transpose(_columns);
            rows.AddRange(_invalidRows);

            if (_shouldShuffleRows)
            {
                if (_options.Header is AssertCsvHeader.Present)
                {
                    List<string> headers = rows[0];
                    List<List<string>> shuffled = Bogus.Random.Shuffle(rows.Skip(1)).ToList();
                    rows = shuffled.Prepend(headers).ToList();
                }
                else
                {
                    rows = Bogus.Random.Shuffle(rows).ToList();
                }
            }

            string csv =
                rows.Select(row => string.Join(Separator, row))
                     .Aggregate((row1, row2) => row1 + NewLine + row2);

            return csv;
        }

        private static List<List<string>> Transpose(List<List<string>> rows)
        {
            if (rows[0].Count <= 0)
            {
                return new List<List<string>>();
            }

            List<string> head = rows.Select(row => row[0]).ToList();
            List<List<string>> tail = Transpose(rows.Select(row => row.Skip(1).ToList()).ToList());

            return tail.Prepend(head).Select(col => col.ToList()).ToList();
        }
    }
}
