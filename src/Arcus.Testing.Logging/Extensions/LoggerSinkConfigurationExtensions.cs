using System;
using GuardNet;
using Serilog;
using Serilog.Configuration;
using Xunit.Abstractions;

namespace Arcus.Testing.Logging.Extensions
{
    /// <summary>
    /// Extensions on the <see cref="LoggerSinkConfiguration"/> to more easily add Serilog sinks related to logging.
    /// </summary>
    public static class LoggerSinkConfigurationExtensions
    {
        /// <summary>
        /// Adds the <see cref="XunitLogEventSink"/> to the Serilog configuration to delegate Serilog log messages to the xUnit test <paramref name="outputWriter"/>.
        /// </summary>
        /// <param name="config">The Serilog sink configuration where the xUnit test logging will be added.</param>
        /// <param name="outputWriter">The xUnit test output writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static LoggerConfiguration XunitTestLogging(
            this LoggerSinkConfiguration config, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config), "Requires a Serilog logger configuration instance to add the xUnit test logging");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a xUnit test output writer to write Serilog log messages to the xUnit test output");

            return config.Sink(new XunitLogEventSink(outputWriter));
        }
    }
}
