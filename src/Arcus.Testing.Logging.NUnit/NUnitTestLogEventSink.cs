using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogEventSink"/> representation of an <see cref="NUnitTestLogger"/> instance.
    /// </summary>
    [Obsolete("Arcus.Testing.Logging.NUnit will stop supporting Serilog by default, please implement Serilog sinks yourself as this sink will be removed in v2.0")]
    public class NUnitTestLogEventSink : ILogEventSink
    {
        private readonly TextWriter _outputWriter, _errorWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogEventSink" /> class.
        /// </summary>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public NUnitTestLogEventSink(TextWriter outputWriter)
        {
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitTestLogEventSink" /> class.
        /// </summary>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <param name="errorWriter">The NUnit test writer to write custom test errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> or the <paramref name="errorWriter"/> is <c>null</c>.</exception>
        public NUnitTestLogEventSink(TextWriter outputWriter, TextWriter errorWriter)
        {
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
            _errorWriter = errorWriter ?? throw new ArgumentNullException(nameof(errorWriter));
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            string message = logEvent.RenderMessage();
            if (logEvent.Level != LogEventLevel.Error)
            {
                _outputWriter.WriteLine("{0:s} {1} > {2}", DateTimeOffset.UtcNow, logEvent.Level, message);
            }
            else
            {
                if (_errorWriter != null)
                {
                    _errorWriter.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logEvent.Level, message, logEvent.Exception);
                }
                else
                {
                    _outputWriter.WriteLine("{0:s} {1} > {2}: {3}", DateTimeOffset.UtcNow, logEvent.Level, message, logEvent.Exception);
                }
            }
        }
    }
}