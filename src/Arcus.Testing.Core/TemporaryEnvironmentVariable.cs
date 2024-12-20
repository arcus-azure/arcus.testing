using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary environment variable that is set for the duration of the test.
    /// </summary>
    public sealed class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _variableName;
        private readonly bool _safeToDeleteOnTeardown;
        private readonly ILogger _logger;

        private TemporaryEnvironmentVariable(string variableName, bool safeToDeleteOnTeardown, ILogger logger)
        {
            _variableName = variableName;
            _safeToDeleteOnTeardown = safeToDeleteOnTeardown;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEnvironmentVariable"/> which sets an environment variable on the system if no such variable exists yet.
        /// </summary>
        /// <remarks>
        ///     The environment variable is considered a secret, so the value will not be exposed to the test logs.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable CreateSecretIfNotExists(string variableName, string variableValue, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("Environment variable name cannot be blank", nameof(variableValue));
            }

            logger ??= NullLogger.Instance;

            logger.LogTrace("[Test:Setup] Create environment secret variable '{Name}'", variableName);
            Environment.SetEnvironmentVariable(variableName, variableValue);
            
            return new TemporaryEnvironmentVariable(variableName, safeToDeleteOnTeardown: true, logger);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEnvironmentVariable"/> which sets an environment variable on the system if no such variable exists yet.
        /// </summary>
        /// <remarks>
        ///     The environment variable is considered a non-secret, so the value will be exposed to the test logs.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable CreateIfNotExists(string variableName, string variableValue, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("Environment variable name cannot be blank", nameof(variableValue));
            }

            logger ??= NullLogger.Instance;

            logger.LogTrace("[Test:Setup] Create environment variable '{Name}'={Value}", variableName, variableValue);
            Environment.SetEnvironmentVariable(variableName, variableValue);

            return new TemporaryEnvironmentVariable(variableName, safeToDeleteOnTeardown: true, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_safeToDeleteOnTeardown)
            {
                _logger.LogTrace("[Test:Teardown] Delete environment secret variable '{Name}'", _variableName);
                Environment.SetEnvironmentVariable(_variableName, null); 
            }
        }
    }
}
