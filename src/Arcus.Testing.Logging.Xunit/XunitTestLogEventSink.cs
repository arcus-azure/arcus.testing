using System;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogEventSink"/> representation of an <see cref="ITestOutputHelper"/> instance.
    /// </summary>
    [Obsolete("Arcus.Testing.Logging.Xunit will stop supporting Serilog by default, please implement Serilog sinks yourself as this sink will be removed in v2.0")]
    public class XunitLogEventSink : ILogEventSink
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitLogEventSink"/> class.
        /// </summary>
        /// <param name="outputWriter">The xUnit test output writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public XunitLogEventSink(ITestOutputHelper outputWriter)
        {
            if (outputWriter is null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            _outputWriter.WriteLine(logEvent.RenderMessage());
        }
    }
}