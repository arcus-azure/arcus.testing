using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Core
{
    public class TemporaryEnvironmentVariableTests
    {
        private readonly ILogger _logger;
        
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryEnvironmentVariableTests" /> class.
        /// </summary>
        public TemporaryEnvironmentVariableTests(ILogger logger)
        {
            _logger = logger;
        }

        [Fact]
        public void CreateEnvVar_WithoutExistingVar_SucceedsByRemovingVarAfterwards()
        {
            // Arrange
            using var context = new EnvVarTestContext();

            string name = context.WhenNonExistingEnvVar();
            string value = Bogus.Random.Guid().ToString();

            // Act
            TemporaryEnvironmentVariable var = CreateEnvVarIfNotExists(name, value);

            // Assert
            context.ShouldStoreEnvVar(name, value);
            var.Dispose();
            context.ShouldNotStoreEnvVar(name);
        }

        private TemporaryEnvironmentVariable CreateEnvVarIfNotExists(string name, string value)
        {
            return Bogus.Random.Bool()
                ? TemporaryEnvironmentVariable.CreateIfNotExists(name, value, _logger)
                : TemporaryEnvironmentVariable.CreateSecretIfNotExists(name, value, _logger);
        }

        private class EnvVarTestContext : IDisposable
        {
            private readonly Collection<string> _variableNames = new();

            public (string name, string value) WhenExistingEnvVar()
            {
                string name = WhenNonExistingEnvVar();
                string value = Bogus.Random.Guid().ToString();

                Environment.SetEnvironmentVariable(name, value);
                return (name, value);
            }

            public string WhenNonExistingEnvVar()
            {
                string name = Bogus.Random.String2(10);
                _variableNames.Add(name);

                return name;
            }

            public void ShouldStoreEnvVar(string name, string expectedValue)
            {
                string actualValue = Environment.GetEnvironmentVariable(name);
                Assert.True(actualValue is not null, $"there should be an environment variable with the name '{name}', but there wasn't");
                Assert.Equal(expectedValue, actualValue);
            }

            public void ShouldNotStoreEnvVar(string name)
            {
                string value = Environment.GetEnvironmentVariable(name);
                Assert.True(value is null, $"there should not be an environment variable with the name '{name}', but there was");
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                Assert.All(_variableNames, name => Environment.SetEnvironmentVariable(name, null));
            }
        }
    }
}
