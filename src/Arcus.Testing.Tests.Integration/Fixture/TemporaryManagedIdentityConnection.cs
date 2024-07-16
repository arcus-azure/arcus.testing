using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Fixture
{
    internal class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        private TemporaryManagedIdentityConnection(string clientId, TemporaryEnvironmentVariable[] environmentVariables)
        {
            _environmentVariables = environmentVariables;
            ClientId = clientId;
        }

        public string ClientId { get; }

        public static TemporaryManagedIdentityConnection Create(ServicePrincipal servicePrincipal)
        {
            var environmentVariables = new[]
            {
                TemporaryEnvironmentVariable.Create("AZURE_TENANT_ID", servicePrincipal.TenantId),
                TemporaryEnvironmentVariable.Create("AZURE_CLIENT_ID", servicePrincipal.ClientId),
                TemporaryEnvironmentVariable.Create("AZURE_CLIENT_SECRET", servicePrincipal.ClientSecret)
            };

            return new TemporaryManagedIdentityConnection(servicePrincipal.ClientId, environmentVariables);
        }

        public void Dispose()
        {
            Assert.All(_environmentVariables, variable => variable.Dispose());
        }
    }
}
