using System;
using Microsoft.Extensions.Logging;
using ITUnitLogger = TUnit.Core.Logging.ILogger;
using TUnitLogLevel = TUnit.Core.Logging.LogLevel;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogger"/> representation of a TUnit logger.
    /// </summary>
    public class TUnitTestLogger : ILogger
    {
        private readonly ITUnitLogger _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TUnitTestLogger"/> class.
        /// </summary>
        /// <param name="outputWriter">The TUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public TUnitTestLogger(ITUnitLogger outputWriter)
        {
            ArgumentNullException.ThrowIfNull(outputWriter);
            _outputWriter = outputWriter;
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
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            TUnitLogLevel level = ConvertToTUnitLogLevel(logLevel);

            _outputWriter.Log(level, state, exception, formatter);
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">Level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            TUnitLogLevel level = ConvertToTUnitLogLevel(logLevel);
            return _outputWriter.IsEnabled(level);
        }

        private static TUnitLogLevel ConvertToTUnitLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => TUnitLogLevel.Critical,
                LogLevel.Error => TUnitLogLevel.Error,
                LogLevel.Warning => TUnitLogLevel.Warning,
                LogLevel.Information => TUnitLogLevel.Information,
                LogLevel.Debug => TUnitLogLevel.Debug,
                LogLevel.Trace => TUnitLogLevel.Trace,
                LogLevel.None => TUnitLogLevel.None,
                _ => TUnitLogLevel.None
            };
        }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <returns>An <see cref="IDisposable" /> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="ILogger"/> representation of a TUnit logger.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    public class TUnitTestLogger<TCategoryName> : TUnitTestLogger, ILogger<TCategoryName>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TUnitTestLogger"/> class.
        /// </summary>
        public TUnitTestLogger(ITUnitLogger outputWriter) : base(outputWriter)
        {
        }
    }
}
