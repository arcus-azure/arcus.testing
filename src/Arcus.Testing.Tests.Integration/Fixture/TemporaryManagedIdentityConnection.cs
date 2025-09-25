using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Arcus.Testing.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

[assembly: AssemblyFixture(typeof(TemporaryManagedIdentityConnection))]

namespace Arcus.Testing.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary managed identity authentication that is set for the duration of the test.
    /// </summary>
    public sealed class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryManagedIdentityConnection"/> class.
        /// </summary>
        public TemporaryManagedIdentityConnection()
        {
            var configuration = TestConfig.Create();
            var logger = NullLogger.Instance;

            ServicePrincipal servicePrincipal = configuration.GetServicePrincipal();
            _environmentVariables = servicePrincipal.IsDefault
                ? []
                :
                [
                    TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_TENANT_ID", servicePrincipal.TenantId, logger),
                    TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_CLIENT_ID", servicePrincipal.ClientId, logger),
                    TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_CLIENT_SECRET", servicePrincipal.ClientSecret, logger)
                ];
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Assert.All(_environmentVariables, variable => variable.Dispose());
        }
    }
}
