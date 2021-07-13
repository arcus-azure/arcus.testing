﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GuardNet;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Testing.Logging
{
    /// <summary>
    /// Represents a logging sink that collects the emitted log events in-memory.
    /// </summary>
    public class InMemoryLogSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> _logEmits = new ConcurrentQueue<LogEvent>();
        
        /// <summary>
        /// Gets the current log emits available on the sink.
        /// </summary>
        public IEnumerable<LogEvent> CurrentLogEmits => _logEmits.ToArray();

        /// <summary>
        /// Gets the current messages of the log emits available on the sink.
        /// </summary>
        public IEnumerable<string> CurrentLogMessages => CurrentLogEmits.Select(emit => emit.RenderMessage());
        
        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            Guard.NotNull(logEvent, nameof(logEvent), "Requires a Serilog log event to be collected in-memory");
            _logEmits.Enqueue(logEvent);
        }
    }
}
