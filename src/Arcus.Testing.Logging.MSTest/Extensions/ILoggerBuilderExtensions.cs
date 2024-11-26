using System;
using Arcus.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        /// Adds the logging messages from the given xUnit <paramref name="testContext"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the NUnit logging test messages to.</param>
        /// <param name="testContext">The MSTest writer to write custom test output.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="testContext"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddMSTestLogging(this ILoggingBuilder builder, TestContext testContext)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(testContext);

            var logger = new MSTestLogger(testContext);
            var provider = new CustomLoggerProvider(logger);

            return builder.AddProvider(provider);
        }
    }
}
