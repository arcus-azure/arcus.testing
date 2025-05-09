using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary managed identity authentication that is set for the duration of the test.
    /// </summary>
    internal sealed class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        private TemporaryManagedIdentityConnection(TemporaryEnvironmentVariable[] environmentVariables)
        {
            _environmentVariables = environmentVariables;
        }

        /// <summary>
        /// Creates a new <see cref="TemporaryManagedIdentityConnection"/> instance for a specific <paramref name="configuration"/>
        /// using the current registration of the service principal.
        /// </summary>
        public static TemporaryManagedIdentityConnection Create(TestConfig configuration, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            logger ??= NullLogger.Instance;

            ServicePrincipal servicePrincipal = configuration.GetServicePrincipal();
            if (servicePrincipal.IsDefault)
            {
                logger.LogTrace("[Test:Setup] no local service principal was registered in the test configuration 'appsettings.*.json', which means managed identity relies on local authenticated sessions (ex. VisualStudio account, Azure CLI...)");
                return new TemporaryManagedIdentityConnection([]);
            }

            return new TemporaryManagedIdentityConnection(
            [
                TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_TENANT_ID", servicePrincipal.TenantId, logger),
                TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_CLIENT_ID", servicePrincipal.ClientId, logger),
                TemporaryEnvironmentVariable.SetSecretIfNotExists("AZURE_CLIENT_SECRET", servicePrincipal.ClientSecret, logger)
            ]);
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
