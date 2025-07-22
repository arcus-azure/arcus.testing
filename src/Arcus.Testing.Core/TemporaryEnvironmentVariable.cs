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

        private bool _isDisposed;

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
            ArgumentException.ThrowIfNullOrWhiteSpace(variableName);
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
                    logger.LogSetupNewVariable(variableName, variableValue);
                    break;

                case (null, true):
                    logger.LogSetupNewSecretVariable(variableName);
                    break;

                case (_, false):
                    logger.LogSetupOverrideVariable(variableName, currentValue, variableValue);
                    break;

                case (_, true):
                    logger.LogSetupOverrideSecretVariable(variableName);
                    break;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            switch (_originalValue, _isSecret)
            {
                case (null, false):
                    _logger.LogTeardownRemoveVariable(_variableName, _currentValue);
                    break;

                case (null, true):
                    _logger.LogTeardownRemoveSecretVariable(_variableName);
                    break;

                case (_, false):
                    _logger.LogTeardownRevertVariable(_variableName, _currentValue, _originalValue);
                    break;

                case (_, true):
                    _logger.LogTeardownRevertSecretVariable(_variableName);
                    break;
            }

            Environment.SetEnvironmentVariable(_variableName, _originalValue);
        }
    }

    internal static partial class TempEnvILoggerExtensions
    {
        private const LogLevel SetupTeardownLogLevel = LogLevel.Debug;

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Set new environment variable '{Name}' with '{Value}'")]
        internal static partial void LogSetupNewVariable(this ILogger logger, string name, string value);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Set new secret environment variable '{Name}'")]
        internal static partial void LogSetupNewSecretVariable(this ILogger logger, string name);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Override environment variable '{Name}' from '{CurrentValue}' to '{NewValue}'")]
        internal static partial void LogSetupOverrideVariable(this ILogger logger, string name, string currentValue, string newValue);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Setup] Override secret environment variable '{Name}' to new value")]
        internal static partial void LogSetupOverrideSecretVariable(this ILogger logger, string name);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Remove environment variable '{Name}' with '{Value}'")]
        internal static partial void LogTeardownRemoveVariable(this ILogger logger, string name, string value);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Remove secret environment variable '{Name}'")]
        internal static partial void LogTeardownRemoveSecretVariable(this ILogger logger, string name);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert environment variable '{Name}' from '{CurrentValue}' back to '{OriginalValue}'")]
        internal static partial void LogTeardownRevertVariable(this ILogger logger, string name, string currentValue, string originalValue);

        [LoggerMessage(
            Level = SetupTeardownLogLevel,
            Message = "[Test:Teardown] Revert secret environment variable '{Name}' back to original value")]
        internal static partial void LogTeardownRevertSecretVariable(this ILogger logger, string name);
    }
}
