using System;
using System.Linq;
using System.Text;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the position in the input document that should be included in the failure report.
    /// </summary>
    public enum ReportScope
    {
        /// <summary>
        /// Limit the reported input document to the element, tag, value... that differs - only a portion of the input document will be shown where the difference resides.
        /// Useful for bigger documents where it would be hard to see the difference in the full document.
        /// </summary>
        /// <remarks>
        ///     Will be truncated based on the configured maximum input characters.
        ///     Also note that a bigger 'context' of the input document will be included in case the difference itself is too narrow to make decisions (ex. integer difference without the name of element or tag).
        /// </remarks>
        Limited = 0,
        
        /// <summary>
        /// Include the complete input document in the failure report.
        /// </summary>
        /// <remarks>
        ///     Will be truncated based on the configured maximum input characters.
        /// </remarks>
        Complete
    }

    /// <summary>
    /// Represents the format in which the different input documents will be shown in the failure report.
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>
        /// Place the expected-actual documents horizontally next to each other.
        /// Useful for documents that expand in length instead of width to see the difference more clearly (i.e. XML, JSON...).
        /// </summary>
        Horizontal = 0,
        
        /// <summary>
        /// Place the expected-actual documents vertically below each other.
        /// Useful for documents that expand in the width instead of length to see the difference more clearly (i.e. CSV, ...).
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Represents the additional options to configure the failure report written to the test output.
    /// </summary>
    internal class ReportOptions
    {
        /// <summary>
        /// Gets or sets the maximum characters of the expected and actual inputs should be written to the test output.
        /// </summary>
        internal int MaxInputCharacters { get; set; } = 500;

        /// <summary>
        /// Gets or sets the position in the input document that should be included in the failure report.
        /// </summary>
        internal ReportScope Scope { get; set; } = ReportScope.Limited;

        /// <summary>
        /// Gets or sets the format in which the different input documents will be shown in the failure report.
        /// </summary>
        internal ReportFormat Format { get; set; } = ReportFormat.Horizontal;
    }

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
        internal ReportBuilder AppendLine(string message)
        {
            _report.AppendLine(message);
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
        /// <param name="configureOptions">The additional options to configure the failure report.</param>
        internal ReportBuilder AppendDiff(string expected, string actual, Action<ReportOptions> configureOptions = null)
        {
            var options = new ReportOptions();
            configureOptions?.Invoke(options);

            if (options.MaxInputCharacters <= 0)
            {
                return this;
            }

            _report.AppendLine();

            string[] expectedLines = SplitLines(expected, "Expected:", options);
            string[] actualLines = SplitLines(actual, "Actual:", options);

            string[] diff = options.Format switch
            {
                ReportFormat.Horizontal => PlaceDiffHorizontally(expectedLines, actualLines),
                ReportFormat.Vertical => PlaceDiffVertically(expectedLines, actualLines),
                _ => throw new ArgumentOutOfRangeException(nameof(configureOptions), options.Format, "Cannot create failure report as an unknown report format is configured in the assert options")
            };

            _report.AppendJoin(Environment.NewLine, diff);
            _report.AppendLine();

            return this;
        }

        private static string[] PlaceDiffVertically(string[] expectedLines, string[] actualLines)
        {
            if (!string.IsNullOrWhiteSpace(expectedLines[^1]))
            {
                expectedLines = expectedLines.Append(string.Empty).ToArray();
            }

            return expectedLines.Concat(actualLines)
                                .ToArray();
        }

        private static string[] PlaceDiffHorizontally(string[] expectedLines, string[] actualLines)
        {
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

        private static string[] SplitLines(string input, string title, ReportOptions options)
        {
            var lines = Truncate(input, options).Split(Environment.NewLine).Prepend(title).ToArray();
            return lines;
        }

        private static string Truncate(string txt, ReportOptions options)
        {
            switch (options.Scope)
            {
                case ReportScope.Complete:
                    return txt;

                case ReportScope.Limited:
                    var result = new StringBuilder();
                    var current = 0;

                    var suffix = string.Empty;
                    foreach (char ch in txt)
                    {
                        if (current >= options.MaxInputCharacters)
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
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(options), options.Scope, "Cannot create failure report as an unknown report scope is configured in the assert options");
            }
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