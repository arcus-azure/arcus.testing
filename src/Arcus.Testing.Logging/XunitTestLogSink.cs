using System;
using GuardNet;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Arcus.Testing.Logging
{
    /// <summary>
    /// <see cref="ILogEventSink"/> representation of an <see cref="ITestOutputHelper"/> instance.
    /// </summary>
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
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a xUnit test output writer to write Serilog log messages to the xUnit test output");
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
