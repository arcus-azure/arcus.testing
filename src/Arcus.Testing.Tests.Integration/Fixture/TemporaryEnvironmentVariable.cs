using System;

namespace Arcus.Testing.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary environment variable that is set for the duration of the test.
    /// </summary>
    public class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _variableName;

        private TemporaryEnvironmentVariable(string variableName)
        {
            _variableName = variableName;
        }

        /// <summary>
        /// Creates a <see cref="TemporaryEnvironmentVariable"/> test fixture.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable Create(string variableName, string variableValue)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("Environment variable name cannot be blank", nameof(variableValue));
            }

            Environment.SetEnvironmentVariable(variableName, variableValue);
            return new TemporaryEnvironmentVariable(variableName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_variableName, null);
        }
    }
}