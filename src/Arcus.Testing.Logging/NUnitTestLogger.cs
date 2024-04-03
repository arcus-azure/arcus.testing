using System;
using System.IO;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing.Logging
{
    /// <summary>
    /// <see cref="ILogger"/> representation of a NUnit logger.
    /// </summary>
    public class NUnitTestLogger : ILogger
    {
        private readonly TextWriter _testContextOut, _testContextError;

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogger" /> class.
        /// </summary>
        /// <param name="testContextOut">The writer of the test context of the current test that will send output to the current test result. <code>TestContext.Out</code></param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="testContextOut"/> is <c>null</c>.</exception>
        public NUnitTestLogger(TextWriter testContextOut)
        {
            Guard.NotNull(testContextOut, nameof(testContextOut));
            _testContextOut = testContextOut;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogger" /> class.
        /// </summary>
        /// <param name="testContextOut">The writer of the test context of the current test that will send output to the current test result. <code>TestContext.Out</code></param>
        /// <param name="testContextError">The writer of the text context of the current test that will send output directly to the console error stream. <code>TestContext.Error</code></param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="testContextOut"/> or the <paramref name="testContextError"/> is <c>null</c>.</exception>
        public NUnitTestLogger(TextWriter testContextOut, TextWriter testContextError)
        {
            Guard.NotNull(testContextOut, nameof(testContextOut));
            Guard.NotNull(testContextError, nameof(testContextError));

            _testContextOut = testContextOut;
            _testContextError = testContextError;
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            if (logLevel != LogLevel.Error)
            {
                _testContextOut.WriteLine("{0:s} {1} > {2}", DateTimeOffset.UtcNow, logLevel, message);
            }
            else
            {
                if (_testContextError != null)
                {
                    _testContextError.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logLevel, message, exception);
                }
                else
                {
                    _testContextOut.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logLevel, message, exception);
                }
            }
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <returns>An <see cref="T:System.IDisposable" /> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="ILogger"/> representation of a NUnit logger.
    /// </summary>
    /// <typeparam name="TCategoryName">The type who's name is used for the logger category name.</typeparam>
    public class NUnitTestLogger<TCategoryName> : NUnitTestLogger, ILogger<TCategoryName>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogger" /> class.
        /// </summary>
        /// <param name="testContextOut">The writer of the test context of the current test that will send output to the current test result. <code>TestContext.Out</code></param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="testContextOut"/> is <c>null</c>.</exception>
        public NUnitTestLogger(TextWriter testContextOut) : base(testContextOut)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogger" /> class.
        /// </summary>
        /// <param name="testContextOut">The writer of the test context of the current test that will send output to the current test result. <code>TestContext.Out</code></param>
        /// <param name="testContextError">The writer of the text context of the current test that will send output directly to the console error stream. <code>TestContext.Error</code></param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="testContextOut"/> or the <paramref name="testContextError"/> is <c>null</c>.</exception>
        public NUnitTestLogger(TextWriter testContextOut, TextWriter testContextError) : base(testContextOut, testContextError)
        {
        }
    }
}
