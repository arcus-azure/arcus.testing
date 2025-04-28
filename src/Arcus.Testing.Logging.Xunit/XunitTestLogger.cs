using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogger"/> representation of a xUnit <see cref="ITestOutputHelper"/> logger.
    /// </summary>
    public class XunitTestLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitTestLogger"/> class.
        /// </summary>
        /// <param name="testOutput">The xUnit test output logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="testOutput"/> is <c>null</c>.</exception>
        public XunitTestLogger(ITestOutputHelper testOutput)
        {
            ArgumentNullException.ThrowIfNull(testOutput);
            _testOutput = testOutput;
        }

        /// <summary>Writes a log entry.</summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            if (exception is null)
            {
                _testOutput.WriteLine("{0:s} {1} > {2}", DateTimeOffset.UtcNow, logLevel, message);
            }
            else
            {
                _testOutput.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logLevel, message, exception);
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

        /// <summary>Begins a logical operation scope.</summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="ILogger"/> representation of a xUnit <see cref="ITestOutputHelper"/> logger.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    public class XunitTestLogger<TCategoryName> : XunitTestLogger, ILogger<TCategoryName>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XunitTestLogger"/> class.
        /// </summary>
        /// <param name="testOutput">The xUnit test output logger.</param>
        public XunitTestLogger(ITestOutputHelper testOutput) : base(testOutput)
        {
        }
    }
}
