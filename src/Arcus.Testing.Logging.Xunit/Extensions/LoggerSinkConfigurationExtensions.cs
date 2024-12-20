using System;
using Arcus.Testing;
using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace Serilog.Configuration
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
        [Obsolete("Arcus.Testing.Logging.Xunit will stop supporting Serilog by default, please implement Serilog sinks yourself as this extension will be removed in v2.0")]
        public static LoggerConfiguration XunitTestLogging(
            this LoggerSinkConfiguration config, 
            ITestOutputHelper outputWriter)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (outputWriter is null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            return config.Sink(new XunitLogEventSink(outputWriter));
        }
    }
}
