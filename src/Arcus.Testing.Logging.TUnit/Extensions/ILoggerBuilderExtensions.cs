using System;
using Arcus.Testing;
using ITUnitLogger = TUnit.Core.Logging.ILogger;

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
        /// Adds the logging messages from the given TUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the NUnit logging test messages to.</param>
        /// <param name="outputWriter">The TUnit test writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddTUnitTestLogging(this ILoggingBuilder builder, ITUnitLogger outputWriter)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(outputWriter);

#pragma warning disable CA2000 // Responsibility of disposing the created object is transferred to the caller
            var provider = new TUnitLoggerProvider(outputWriter);
#pragma warning restore CA2000
            return builder.AddProvider(provider);
        }

        [ProviderAlias("TUnit")]
        private sealed class TUnitLoggerProvider : ILoggerProvider, ISupportExternalScope
        {
            private readonly ITUnitLogger _testLogger;
            private IExternalScopeProvider _scopeProvider;

            /// <summary>
            /// Initializes a new instance of the <see cref="TUnitLoggerProvider"/> class.
            /// </summary>
            internal TUnitLoggerProvider(ITUnitLogger testLogger)
            {
                _testLogger = testLogger;
            }

            /// <summary>
            /// Creates a new <see cref="ILogger" /> instance.
            /// </summary>
            /// <param name="categoryName">The category name for messages produced by the logger.</param>
            /// <returns>The instance of <see cref="ILogger" /> that was created.</returns>
            public ILogger CreateLogger(string categoryName)
            {
                return new TUnitTestLogger(_testLogger, _scopeProvider, categoryName);
            }

            /// <summary>
            /// Sets external scope information source for logger provider.
            /// </summary>
            /// <param name="scopeProvider">The provider of scope data.</param>
            public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                _scopeProvider = scopeProvider;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
