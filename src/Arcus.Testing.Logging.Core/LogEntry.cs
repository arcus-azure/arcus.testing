using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a logged message by a <see cref="ILogger"/> instance.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry" /> class.
        /// </summary>
        /// <param name="id">The ID of the event.</param>
        /// <param name="level">The entry will be written on this level.</param>
        /// <param name="message">The formatted message of this entry.</param>
        /// <param name="exception">The exception related to this entry.</param>
        public LogEntry(EventId id, LogLevel level, string message, Exception exception)
        {
            Level = level;
            Id = id;
            Exception = exception;
            Message = message;
        }

        /// <summary>
        /// Gets the event ID category of the log entry.
        /// </summary>
        public EventId Id { get; }

        /// <summary>
        /// Gets the level on which the <see cref="Message"/> is logged.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the formatted message that is logged.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the optional exception that was related to this log entry.
        /// </summary>
        public Exception Exception { get; }
    }
}