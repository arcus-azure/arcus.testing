using System;

namespace Arcus.Testing.Tests.Integration.Fixture
{
    public class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _variableName;

        private TemporaryEnvironmentVariable(string variableName)
        {
            _variableName = variableName;
        }

        public static TemporaryEnvironmentVariable Create(string variableName, string variableValue)
        {
            Environment.SetEnvironmentVariable(variableName, variableValue);
            return new TemporaryEnvironmentVariable(variableName);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_variableName, null);
        }
    }
}
