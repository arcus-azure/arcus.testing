using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using FsCheck;
using Moq;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_.Fixture
{
    public class TestCsvOptions : AssertCsvOptions
    {
        private static readonly Faker Bogus = new();

        public int LineCount { get; set; } = Bogus.Random.Int(1, 10);

        public int ColumnCount { get; set; } = Bogus.Random.Int(1, 10);
    }

    /// <summary>
    /// Represents a test fixture that generates random CSV documents.
    /// </summary>
    public class TestCsv
    {
        private readonly TestCsvOptions _options;
        private readonly List<List<string>> _columns;
        private readonly List<List<string>> _invalidLines = new();
        private static readonly Faker Bogus = new();
        private bool _shouldShuffle;

        private TestCsv(IEnumerable<string[]> columns, string[] headerNames, TestCsvOptions options)
        {
            _options = options;
            _columns = columns.Select(col => col.ToList()).ToList();

            HeaderNames = headerNames;
            ColumnCount = options.ColumnCount;
            LineCount = options.LineCount;
        }

        public string[] HeaderNames { get; }
        public string[][] Lines => Transpose(_columns).Select(col => col.ToArray()).ToArray();
        public int ColumnCount { get; private set; }
        public int LineCount { get; private set; }
        public string Separator { get; } = Bogus.PickRandom(";", ",.");
        public string NewLine { get; } = Bogus.PickRandom(Environment.NewLine, "\n");

        public static TestCsv Generate(Action<TestCsvOptions> configureOptions = null)
        {
            var options = new TestCsvOptions();
            configureOptions?.Invoke(options);

            IList<string[]> columns = Bogus.Make(options.ColumnCount, () =>
            {
                string[] col = GenerateColumn(options.LineCount);
                if (options.Header is CsvHeader.Present)
                {
                    return col.Prepend(Bogus.Database.Column()).ToArray();
                }

                return col.ToArray();
            });

            string[] headerNames = 
                options.Header is CsvHeader.Present
                    ? columns.Select(col => col[0]).ToArray()
                    : columns.Select((_, index) => $"Col #{index}").ToArray();

            return new TestCsv(columns, headerNames, options);
        }

        public void AddColumn()
        {
            string columnName = Bogus.Database.Column();
            List<string> newColumn = GenerateColumn(LineCount).Prepend(columnName).ToList();
            _columns.Insert(Bogus.Random.Int(0, _columns.Count - 1), newColumn);
            ColumnCount++;
        }

        public void AddLine()
        {
            Assert.All(_columns, col => col.Add(GenValueFunc()()));
            LineCount++;
        }

        public void AddInvalidLine()
        {
            _invalidLines.Add(Bogus.Make(Bogus.Random.Int(11, 20), GenValue).ToList());
        }

        public string ChangeColumnValue()
        {
            List<string> col = Bogus.PickRandom(_columns);
            string original = col[0];
            col[0] = "changed-" + Bogus.Database.Column();

            return original;
        }

        public string ChangeCellValue()
        {
            List<string> col = Bogus.PickRandom(_columns);
            int index = Bogus.Random.Int(1, col.Count - 1);
            
            string changedValue = GenValue();
            col[index] = "diff-" + changedValue;

            return changedValue;
        }

        public void Shuffle()
        {
            _shouldShuffle = true;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public TestCsv Copy()
        {
            return new TestCsv(_columns.Select(col => col.ToArray()), HeaderNames, _options);
        }

        private static string[] GenerateColumn(int lineCount)
        {
            Func<string> genValue = GenValueFunc();
            return Bogus.Make(lineCount, genValue).ToArray();
        }

        private static string GenValue()
        {
            return GenValueFunc()();
        }

        private static Func<string> GenValueFunc()
        {
            return Bogus.PickRandom(
                () => Bogus.Lorem.Word(),
                () => Bogus.Random.Int().ToString(),
                () => Bogus.Random.Float() + string.Concat(Bogus.Make(Bogus.Random.Int(1, 10), () => "0")),
                () => Bogus.Date.RecentOffset().ToString());
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            List<List<string>> lines = Transpose(_columns);
            lines.AddRange(_invalidLines);

            if (_shouldShuffle)
            {
                if (_options.Header is CsvHeader.Present)
                {
                    List<string> headers = lines.First();
                    List<List<string>> shuffled = Bogus.Random.Shuffle(lines.Skip(1)).ToList();
                    lines = shuffled.Prepend(headers).ToList();
                }
                else
                {
                    lines = Bogus.Random.Shuffle(lines).ToList();
                }
            }

            string csv = 
                lines.Select(line => string.Join(Separator, line))
                     .Aggregate((line1, line2) => line1 + NewLine + line2);

            return csv;
        }

        private static List<List<string>> Transpose(List<List<string>> lines)
        {
            if (lines[0].Count <= 0)
            {
                return new List<List<string>>();
            }

            List<string> head = lines.Select(line => line[0]).ToList();
            List<List<string>> tail = Transpose(lines.Select(line => line.Skip(1).ToList()).ToList());

            return tail.Prepend(head).Select(col => col.ToList()).ToList();
        }
    }
}
