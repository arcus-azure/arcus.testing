﻿using System;
using Arcus.Testing;
using Xunit.Abstractions;

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
        /// Adds an the logging messages from the given xUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the xUnit logging test messages to.</param>
        /// <param name="outputWriter">The xUnit test logger used across the test suite.</param>
        /// <exception cref="ArgumentNullException">Thrown when either the <paramref name="builder"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddXunitTestLogging(this ILoggingBuilder builder, ITestOutputHelper outputWriter)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (outputWriter is null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            var logger = new XunitTestLogger(outputWriter);
            var provider = new CustomLoggerProvider(logger);

            return builder.AddProvider(provider);
        }
    }
}
