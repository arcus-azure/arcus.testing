using System;
using System.Collections.Generic;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Testing.Security.Providers.InMemory;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions on the <see cref="SecretStoreBuilder"/> class to add test-related options.
    /// </summary>
    public static class SecretStoreBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");

            builder.AddInMemory(secretProviderName: null);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, ICacheConfiguration cacheConfiguration)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            
            builder.AddInMemory(cacheConfiguration, secretProviderName: null);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");

            builder.AddProvider(new InMemorySecretProvider(), options => options.Name = secretProviderName);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, ICacheConfiguration cacheConfiguration, string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNull(cacheConfiguration, nameof(cacheConfiguration), "Requires a configuration instance to describe how the caching of secrets should work");
            
            builder.AddProvider(new InMemoryCachedSecretProvider(cacheConfiguration), options => options.Name = secretProviderName);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, string secretName, string secretValue)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank test secret name to store the secret value in-memory");
            
            builder.AddInMemory(secretName, secretValue, secretProviderName: null);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(
            this SecretStoreBuilder builder, 
            string secretName, 
            string secretValue, 
            ICacheConfiguration cacheConfiguration)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank test secret name to store the secret value in-memory");
            Guard.NotNull(cacheConfiguration, nameof(cacheConfiguration), "Requires a configuration instance to describe how the caching of secrets should work");

            builder.AddInMemory(secretName, secretValue, cacheConfiguration, secretProviderName: null);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, string secretName, string secretValue, string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank test secret name to store the secret value in-memory");
            
            builder.AddProvider(new InMemorySecretProvider(secretName, secretValue), options => options.Name = secretProviderName);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(
            this SecretStoreBuilder builder, 
            string secretName, 
            string secretValue, 
            ICacheConfiguration cacheConfiguration,
            string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank test secret name to store the secret value in-memory");
            Guard.NotNull(cacheConfiguration, nameof(cacheConfiguration), "Requires a configuration instance to describe how the caching of secrets should work");

            builder.AddProvider(new InMemoryCachedSecretProvider(secretName, secretValue, cacheConfiguration), options => options.Name = secretProviderName);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, IDictionary<string, string> secrets)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNull(secrets, nameof(secrets), "Requires a set of secrets to initialize the in-memory secret provider");
            
            builder.AddInMemory(secrets, secretProviderName: null);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="builder"/>, the <paramref name="secrets"/>, or the <paramref name="cacheConfiguration"/> is <c>null</c>.
        /// </exception>
        public static SecretStoreBuilder AddInMemory(
            this SecretStoreBuilder builder, 
            IDictionary<string, string> secrets, 
            ICacheConfiguration cacheConfiguration)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNull(secrets, nameof(secrets), "Requires a set of secrets to initialize the in-memory secret provider");
            Guard.NotNull(cacheConfiguration, nameof(cacheConfiguration), "Requires a configuration instance to describe how the caching of secrets should work");
            
            builder.AddInMemory(secrets, cacheConfiguration, secretProviderName: null);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemorySecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="secrets"/> is <c>null</c>.</exception>
        public static SecretStoreBuilder AddInMemory(this SecretStoreBuilder builder, IDictionary<string, string> secrets, string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNull(secrets, nameof(secrets), "Requires a set of secrets to initialize the in-memory secret provider");
            
            builder.AddProvider(new InMemorySecretProvider(secrets), options => options.Name = secretProviderName);
            return builder;
        }
        
        /// <summary>
        /// Adds the <see cref="InMemoryCachedSecretProvider"/> to the secret store without any in-memory stored secrets.
        /// </summary>
        /// <param name="builder">The secret store builder to add the secret provider.</param>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="secretProviderName">The name to register the secret provider by.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="builder"/>, the <paramref name="secrets"/>, or the <paramref name="cacheConfiguration"/> is <c>null</c>.
        /// </exception>
        public static SecretStoreBuilder AddInMemory(
            this SecretStoreBuilder builder, 
            IDictionary<string, string> secrets, 
            ICacheConfiguration cacheConfiguration,
            string secretProviderName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a secret store builder to add the in-memory secret provider to the secret store");
            Guard.NotNull(secrets, nameof(secrets), "Requires a set of secrets to initialize the in-memory secret provider");
            Guard.NotNull(cacheConfiguration, nameof(cacheConfiguration), "Requires a configuration instance to describe how the caching of secrets should work");

            builder.AddProvider(new InMemoryCachedSecretProvider(secrets, cacheConfiguration), options => options.Name = secretProviderName);
            return builder;
        }
    }
}
