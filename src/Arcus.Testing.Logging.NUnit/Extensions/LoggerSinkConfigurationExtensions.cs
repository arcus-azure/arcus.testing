using System;
using System.IO;
using Arcus.Testing;

// ReSharper disable once CheckNamespace
namespace Serilog.Configuration
{
    /// <summary>
    /// Extensions on the <see cref="LoggerSinkConfiguration"/> to more easily add Serilog sinks related to logging.
    /// </summary>
    public static class LoggerSinkConfigurationExtensions
    {
        /// <summary>
        /// Adds the <see cref="NUnitTestLogEventSink"/> to the Serilog configuration to delegate Serilog log messages to the NUnit test <paramref name="outputWriter"/>.
        /// </summary>
        /// <param name="config">The Serilog sink configuration where the NUnit test logging will be added.</param>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        [Obsolete("Arcus.Testing.Logging.NUnit will stop supporting Serilog by default, please implement Serilog sinks yourself as this extension will be removed in v2.0")]
        public static LoggerConfiguration NUnitTestLogging(this LoggerSinkConfiguration config, TextWriter outputWriter)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (outputWriter is null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            return config.Sink(new NUnitTestLogEventSink(outputWriter));
        }

        /// <summary>
        /// Adds the <see cref="NUnitTestLogEventSink"/> to the Serilog configuration to delegate Serilog log messages to the NUnit test <paramref name="outputWriter"/>.
        /// </summary>
        /// <param name="config">The Serilog sink configuration where the NUnit test logging will be added.</param>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <param name="errorWriter">The NUnit test writer to write custom test errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/>, <paramref name="outputWriter"/> or the <paramref name="errorWriter"/> is <c>null</c>.</exception>
        [Obsolete("Arcus.Testing.Logging.NUnit will stop supporting Serilog by default, please implement Serilog sinks yourself as this extension will be removed in v2.0")]
        public static LoggerConfiguration NUnitTestLogging(this LoggerSinkConfiguration config, TextWriter outputWriter, TextWriter errorWriter)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (outputWriter is null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            if (errorWriter is null)
            {
                throw new ArgumentNullException(nameof(errorWriter));
            }

            return config.Sink(new NUnitTestLogEventSink(outputWriter, errorWriter));
        }
    }
}