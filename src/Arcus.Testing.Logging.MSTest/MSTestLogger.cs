using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogger"/> representation of a MSTest <see cref="TestContext"/>.
    /// </summary>
    public class MSTestLogger : ILogger
    {
        private readonly TestContext _testContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSTestLogger" /> class.
        /// </summary>
        /// <param name="testContext">The MSTest context to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="testContext"/> is <c>null</c>.</exception>
        public MSTestLogger(TestContext testContext)
        {
            ArgumentNullException.ThrowIfNull(testContext);
            _testContext = testContext;
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
            if (exception is null)
            {
                _testContext.WriteLine("{0:s} {1} > {2}", DateTimeOffset.UtcNow, logLevel, message);
            }
            else
            {
                _testContext.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logLevel, message, exception);
            }
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">Level to be checked.</param>
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
        /// <returns>An <see cref="IDisposable" /> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="ILogger"/> representation of a MSTest <see cref="TestContext"/>.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    public class MSTestLogger<TCategoryName> : MSTestLogger, ILogger<TCategoryName>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MSTestLogger" /> class.
        /// </summary>
        /// <param name="testContext">The MSTest context to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="testContext"/> is <c>null</c>.</exception>
        public MSTestLogger(TestContext testContext) : base(testContext)
        {
        }
    }
}
