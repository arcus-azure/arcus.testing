using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;

namespace Arcus.Testing.Security.Providers.InMemory
{
    /// <summary>
    /// Represents an <see cref="ISecretProvider"/> implementation that stores the secret values in-memory.
    /// </summary>
    public class InMemorySecretProvider : ISecretProvider
    {
        private readonly IDictionary<string, string> _secrets = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySecretProvider"/> class without any secrets.
        /// </summary>
        public InMemorySecretProvider()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySecretProvider"/> class.
        /// </summary>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public InMemorySecretProvider(string secretName, string secretValue)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank test secret name to store the secret value in-memory");

            _secrets = new Dictionary<string, string>
            {
                [secretName] = secretValue
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySecretProvider"/> class.
        /// </summary>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secrets"/> is <c>null</c>.</exception>
        public InMemorySecretProvider(IDictionary<string, string> secrets)
        {
            Guard.NotNull(secrets, nameof(secrets), "Requires a set of secrets to initialize the in-memory secret provider");

            _secrets = secrets;
        }

        /// <summary>
        /// Retrieves the secret value, based on the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public async Task<Secret> GetSecretAsync(string secretName)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to look up the test secret value");

            string secretValue = await GetRawSecretAsync(secretName);
            if (secretValue is null)
            {
                return null;
            }

            return new Secret(secretValue);
        }

        /// <summary>
        /// Retrieves the secret value, based on the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to look up the test secret value");

            if (_secrets.TryGetValue(secretName, out string secretValue))
            {
                return Task.FromResult(secretValue);
            }

            return Task.FromResult<string>(null);
        }
    }
}
