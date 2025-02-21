using System;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Core
{
    public class TemporaryEnvironmentVariableTests
    {
        private static readonly Faker Bogus = new();

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateEnvVar_WithoutName_Fails(string name)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => TemporaryEnvironmentVariable.SetSecretIfNotExists(name, Bogus.Random.Guid().ToString(), NullLogger.Instance));
        }
    }
}
