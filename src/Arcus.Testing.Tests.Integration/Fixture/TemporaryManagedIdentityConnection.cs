using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary managed identity authentication that is set for the duration of the test.
    /// </summary>
    internal class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        private TemporaryManagedIdentityConnection(string clientId, TemporaryEnvironmentVariable[] environmentVariables)
        {
            _environmentVariables = environmentVariables;
            ClientId = clientId;
        }

        /// <summary>
        /// Gets the client ID of the temporary managed identity authentication.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Creates a new <see cref="TemporaryManagedIdentityConnection"/> instance for a specific <paramref name="servicePrincipal"/>.
        /// </summary>
        /// <param name="servicePrincipal">The service principal that should be authenticated with the test resources using managed identity.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="servicePrincipal"/> is <c>null</c>.</exception>
        public static TemporaryManagedIdentityConnection Create(ServicePrincipal servicePrincipal)
        {
            if (servicePrincipal is null)
            {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }

            var logger = NullLogger.Instance;
            var environmentVariables = new[]
            {
                TemporaryEnvironmentVariable.CreateSecretIfNotExists("AZURE_TENANT_ID", servicePrincipal.TenantId, logger),
                TemporaryEnvironmentVariable.CreateSecretIfNotExists("AZURE_CLIENT_ID", servicePrincipal.ClientId, logger),
                TemporaryEnvironmentVariable.CreateSecretIfNotExists("AZURE_CLIENT_SECRET", servicePrincipal.ClientSecret, logger)
            };

            return new TemporaryManagedIdentityConnection(servicePrincipal.ClientId, environmentVariables);
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
