using System;
using Arcus.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable once CheckNamespace
namespace Serilog.Configuration
{
    /// <summary>
    /// Extensions on the <see cref="LoggerSinkConfiguration"/> to more easily add Serilog sinks related to logging.
    /// </summary>
    public static class LoggerSinkConfigurationExtensions
    {
        /// <summary>
        /// Adds the <see cref="MSTestLogEventSink"/> to the Serilog configuration to delegate Serilog log messages to the MSTest <paramref name="testContext"/>.
        /// </summary>
        /// <param name="config">The Serilog sink configuration where the NUnit test logging will be added.</param>
        /// <param name="testContext">The MSTest test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or <paramref name="testContext"/> is <c>null</c>.</exception>
        [Obsolete("Arcus.Testing.Logging.MSTest will stop supporting Serilog by default, please implement Serilog sinks yourself as this extension will be removed in v2.0")]
        public static LoggerConfiguration MSTestLogging(this LoggerSinkConfiguration config, TestContext testContext)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (testContext is null)
            {
                throw new ArgumentNullException(nameof(testContext));
            }

            return config.Sink(new MSTestLogEventSink(testContext));
        }
    }
}
