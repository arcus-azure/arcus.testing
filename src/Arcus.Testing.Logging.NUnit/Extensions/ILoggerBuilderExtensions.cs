using System;
using System.IO;
using Arcus.Testing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extensions on the <see cref="ILoggingBuilder"/> related to logging.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ILoggerBuilderExtensions
    {
        /// <summary>
        /// Adds the logging messages from the given xUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the NUnit logging test messages to.</param>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddNUnitTestLogging(this ILoggingBuilder builder, TextWriter outputWriter)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(outputWriter);

            var logger = new NUnitTestLogger(outputWriter);
            var provider = new NUnitLoggerProvider(logger);

            return builder.AddProvider(provider);
        }

        /// <summary>
        /// Adds the logging messages from the given xUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the NUnit logging test messages to.</param>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <param name="errorWriter">The NUnit test writer to write custom test errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/>, <paramref name="outputWriter"/> or the <paramref name="errorWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddNUnitTestLogging(this ILoggingBuilder builder, TextWriter outputWriter, TextWriter errorWriter)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(outputWriter);

            var logger = new NUnitTestLogger(outputWriter, errorWriter);
            var provider = new NUnitLoggerProvider(logger);

            return builder.AddProvider(provider);
        }

        [ProviderAlias("NUnit")]
        private sealed class NUnitLoggerProvider : ILoggerProvider
        {
            private readonly NUnitTestLogger _logger;

            public NUnitLoggerProvider(NUnitTestLogger logger)
            {
                _logger = logger;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return _logger;
            }

            public void Dispose()
            {
            }
        }
    }
}
