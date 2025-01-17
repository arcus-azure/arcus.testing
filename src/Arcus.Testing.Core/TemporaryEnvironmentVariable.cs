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
        private readonly string _variableName, _currentValue, _originalValue;
        private readonly bool _isSecret;
        private readonly ILogger _logger;

        private TemporaryEnvironmentVariable(string variableName, string currentValue, string originalValue, bool isSecret, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(variableName);

            _variableName = variableName;
            _currentValue = currentValue;
            _originalValue = originalValue;
            _isSecret = isSecret;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEnvironmentVariable"/> which sets an environment variable on the system if no such variable exists yet.
        /// </summary>
        /// <remarks>
        ///     The environment variable is considered a secret, so the value will not be exposed to the test logs.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable SetSecretIfNotExists(string variableName, string variableValue, ILogger logger)
        {
            return SetIfNotExists(variableName, variableValue, isSecret: true, logger);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryEnvironmentVariable"/> which sets an environment variable on the system if no such variable exists yet.
        /// </summary>
        /// <remarks>
        ///     The environment variable is considered a non-secret, so the value will be exposed to the test logs.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable SetIfNotExists(string variableName, string variableValue, ILogger logger)
        {
            return SetIfNotExists(variableName, variableValue, isSecret: false, logger);
        }

        private static TemporaryEnvironmentVariable SetIfNotExists(
            string variableName,
            string variableValue,
            bool isSecret,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("Environment variable name cannot be blank", nameof(variableValue));
            }

            logger ??= NullLogger.Instance;

            string currentValue = Environment.GetEnvironmentVariable(variableName);
            LogOnSetup(variableName, variableValue, currentValue, isSecret, logger);

            Environment.SetEnvironmentVariable(variableName, variableValue);

            return new TemporaryEnvironmentVariable(variableName, variableValue, originalValue: currentValue, isSecret, logger);
        }

        private static void LogOnSetup(
            string variableName,
            string variableValue,
            string currentValue,
            bool isSecret,
            ILogger logger)
        {
            switch (currentValue, isSecret)
            {
                case (null, false):
                    logger.LogDebug("[Test:Setup] Set new environment variable '{Name}' with '{Value}'", variableName, variableValue);
                    break;

                case (null, true):
                    logger.LogDebug("[Test:Setup] Set new secret environment variable '{Name}'", variableName);
                    break;

                case (_, false):
                    logger.LogDebug("[Test:Setup] Override environment variable '{Name}' from '{CurrentValue}' to '{NewValue}'", variableName, currentValue, variableValue);
                    break;

                case (_, true):
                    logger.LogDebug("[Test:Setup] Override secret environment variable '{Name}' to new value", variableName);
                    break;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            switch (_originalValue, _isSecret)
            {
                case (null, false):
                    _logger.LogDebug("[Test:Teardown] Remove environment variable '{Name}' with '{Value}'", _variableName, _currentValue);
                    break;

                case (null, true):
                    _logger.LogDebug("[Test:Teardown] Remove secret environment variable '{Name}'", _variableName);
                    break;

                case (_, false):
                    _logger.LogDebug("[Test:Teardown] Revert environment variable '{Name}' from '{CurrentValue}' back to '{OriginalValue}'", _variableName, _currentValue, _originalValue);
                    break;

                case (_, true):
                    _logger.LogDebug("[Test:Teardown] Revert secret environment variable '{Name}' back to original value", _variableName);
                    break;
            }

            Environment.SetEnvironmentVariable(_variableName, _originalValue);
        }
    }
}
