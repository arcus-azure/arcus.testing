using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Testing
{
    /// <summary>
    /// <see cref="ILogEventSink"/> representation of an <see cref="MSTestLogger"/> instance.
    /// </summary>
    [Obsolete("Arcus.Testing.Logging.MSTest will stop supporting Serilog by default, please implement Serilog sinks yourself as this sink will be removed in v2.0")]
    public class MSTestLogEventSink : ILogEventSink
    {
        private readonly TestContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSTestLogEventSink" /> class.
        /// </summary>
        /// <param name="context">The MSTest context to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c>.</exception>
        public MSTestLogEventSink(TestContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            _context.WriteLine(logEvent.RenderMessage());
        }
    }
}
