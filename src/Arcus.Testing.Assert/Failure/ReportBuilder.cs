using System;
using System.Linq;
using System.Text;

namespace Arcus.Testing.Failure
{
    /// <summary>
    /// Represents a buildable humanly-readable report of a test assertion failure.
    /// </summary>
    internal class ReportBuilder
    {
        private readonly StringBuilder _report;

        /// <summary>
        /// Gets the default maximum characters of the input should be written to the test output.
        /// </summary>
        internal const int DefaultMaxInputCharacters = 500;

        private ReportBuilder(string methodName, string generalMessage)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Requires a non-blank method name for the test assertion report", nameof(methodName));
            }

            if (string.IsNullOrWhiteSpace(generalMessage))
            {
                throw new ArgumentException("Requires a non-blank general message for the test assertion report", nameof(generalMessage));
            }

            _report = new StringBuilder();
            _report.AppendLine($"{methodName} failure: {generalMessage}");
        }

        /// <summary>
        /// Starts a new report for a specific test assertion.
        /// </summary>
        /// <param name="methodName">The test assertion method name.</param>
        /// <param name="generalMessage">The general description of the test assertion failure.</param>
        internal static ReportBuilder ForMethod(string methodName, string generalMessage)
        {
            return new ReportBuilder(methodName, generalMessage);
        }

        /// <summary>
        /// Appends a new line to the test report.
        /// </summary>
        internal ReportBuilder AppendLine(string message, int maxCharacters = 1000)
        {
            _report.AppendLine(Truncate(message, maxCharacters));
            return this;
        }

        /// <summary>
        /// Appends a new line to the test report.
        /// </summary>
        internal ReportBuilder AppendLine()
        {
            _report.AppendLine();
            return this;
        }

        /// <summary>
        /// Appends the test input of the test assertion method to the report.
        /// </summary>
        /// <param name="input">The user input that was passed to the test assertion.</param>
        internal ReportBuilder AppendInput(string input)
        {
            _report.AppendLine();
            _report.AppendLine("Input:");
            _report.AppendLine(input);

            return this;
        }

        /// <summary>
        /// Appends the expected and actual difference to the test assertion report.
        /// </summary>
        /// <param name="expected">The value that was expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="maxCharacters">The maximum amount of characters to take from either input.</param>
        internal ReportBuilder AppendDiff(string expected, string actual, int maxCharacters = DefaultMaxInputCharacters)
        {
            if (maxCharacters <= 0)
            {
                return this;
            }

            _report.AppendLine();

            string[] diff = PlaceDiffHorizontally(expected, actual, maxCharacters);

            _report.AppendJoin(Environment.NewLine, diff);
            _report.AppendLine();

            return this;
        }

        private static string[] PlaceDiffHorizontally(string expected, string actual, int maxCharacters)
        {
            string[] expectedLines = Truncate(expected, maxCharacters).Split(Environment.NewLine).Prepend("Expected:").ToArray();
            string[] actualLines = Truncate(actual, maxCharacters).Split(Environment.NewLine).Prepend("Actual:").ToArray();

            if (expectedLines.Length != actualLines.Length)
            {
                if (expectedLines.Length > actualLines.Length)
                {
                    actualLines = AddEmptyPadding(actualLines, expectedLines.Length - actualLines.Length);
                }
                else
                {
                    expectedLines = AddEmptyPadding(expectedLines, actualLines.Length - expectedLines.Length);
                }
            }

            int maxColumn = expectedLines.MaxBy(l => l.Length).Length;
            expectedLines = EnsureSameLineLengths(expectedLines, maxColumn);

            const string spaceBetween = "    ";
            string[] diff = expectedLines.Zip(actualLines, (expectedLine, actualLine) => expectedLine + spaceBetween + actualLine).ToArray();
            
            return diff;
        }

        private static string Truncate(string txt, int maxCharacters)
        {
            var result = new StringBuilder();
            var current = 0;

            string suffix = string.Empty;
            foreach (char ch in txt)
            {
                if (current >= maxCharacters)
                {
                    suffix = "...";
                    break;
                }

                if (!char.IsWhiteSpace(ch))
                {
                    current++;
                }

                result.Append(ch);
            }

            result.AppendLine(suffix);
            return result.ToString();
        }

        private static string[] AddEmptyPadding(string[] lines, int amount)
        {
            return lines.Concat(Enumerable.Repeat(string.Empty, amount)).ToArray();
        }

        private static string[] EnsureSameLineLengths(string[] lines, int length)
        {
            return lines.Select(l => l.Length < length ? l + string.Join("", Enumerable.Repeat(" ", length - l.Length)) : l).ToArray();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _report.ToString();
        }
    }
}