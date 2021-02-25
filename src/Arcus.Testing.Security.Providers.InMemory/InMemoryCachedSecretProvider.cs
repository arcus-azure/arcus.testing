using System;
using System.Collections.Generic;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Arcus.Testing.Security.Providers.InMemory
{
    /// <summary>
    /// Represents an <see cref="ICachedSecretProvider"/> that stores the secrets in-memory.
    /// </summary>
    public class InMemoryCachedSecretProvider : CachedSecretProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class without any secrets.
        /// </summary>
        public InMemoryCachedSecretProvider() : base(new InMemorySecretProvider())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public InMemoryCachedSecretProvider(string secretName, string secretValue)
            : base(new InMemorySecretProvider(secretName, secretValue))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secrets"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(IDictionary<string, string> secrets)
            : base(new InMemorySecretProvider(secrets))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class without any secrets.
        /// </summary>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(ICacheConfiguration cacheConfiguration) 
            : base(new InMemorySecretProvider(), cacheConfiguration)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(string secretName, string secretValue, ICacheConfiguration cacheConfiguration)
            : base(new InMemorySecretProvider(secretName, secretValue), cacheConfiguration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secrets"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(IDictionary<string, string> secrets, ICacheConfiguration cacheConfiguration)
            : base(new InMemorySecretProvider(secrets), cacheConfiguration)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class without any secrets.
        /// </summary>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="memoryCache">The <see cref="IMemoryCache" /> implementation that can cache data in memory.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> or <paramref name="memoryCache"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(ICacheConfiguration cacheConfiguration, IMemoryCache memoryCache) 
            : base(new InMemorySecretProvider(), cacheConfiguration, memoryCache)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secretName">The required secret name to store the secret in-memory.</param>
        /// <param name="secretValue">The tested secret value to retrieve upon providing the <paramref name="secretName"/>.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="memoryCache">The <see cref="IMemoryCache" /> implementation that can cache data in memory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> or <paramref name="memoryCache"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(string secretName, string secretValue, ICacheConfiguration cacheConfiguration, IMemoryCache memoryCache)
            : base(new InMemorySecretProvider(secretName, secretValue), cacheConfiguration, memoryCache)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCachedSecretProvider"/> class.
        /// </summary>
        /// <param name="secrets">The set of secret name-value pairs that represents the complete set of available in-memory secrets.</param>
        /// <param name="cacheConfiguration">The <see cref="ICacheConfiguration" /> which defines how the cache works.</param>
        /// <param name="memoryCache">The <see cref="IMemoryCache" /> implementation that can cache data in memory.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secrets"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cacheConfiguration"/> or <paramref name="memoryCache"/> is <c>null</c>.</exception>
        public InMemoryCachedSecretProvider(IDictionary<string, string> secrets, ICacheConfiguration cacheConfiguration, IMemoryCache memoryCache)
            : base(new InMemorySecretProvider(secrets), cacheConfiguration, memoryCache)
        {
        }
    }
}
