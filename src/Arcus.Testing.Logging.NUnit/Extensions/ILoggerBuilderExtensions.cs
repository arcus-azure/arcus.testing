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
        /// Adds the logging messages from the given NUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the NUnit logging test messages to.</param>
        /// <param name="outputWriter">The NUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddNUnitTestLogging(this ILoggingBuilder builder, TextWriter outputWriter)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(outputWriter);

#pragma warning disable CA2000 // Responsibility of disposing the created object is transferred to the caller
            var provider = new NUnitLoggerProvider(outputWriter, errorWriter: null);
#pragma warning restore CA2000

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
            ArgumentNullException.ThrowIfNull(errorWriter);

#pragma warning disable CA2000 // Responsibility of disposing the created object is transferred to the caller
            var provider = new NUnitLoggerProvider(outputWriter, errorWriter);
#pragma warning restore CA2000

            return builder.AddProvider(provider);
        }

        [ProviderAlias("NUnit")]
        private sealed class NUnitLoggerProvider(TextWriter outputWriter, TextWriter errorWriter) : ILoggerProvider, ISupportExternalScope
        {
            private IExternalScopeProvider _scopeProvider;

            public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                _scopeProvider = scopeProvider;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new NUnitTestLogger(outputWriter, errorWriter, _scopeProvider, categoryName);
            }

            public void Dispose()
            {
            }
        }
    }
}
