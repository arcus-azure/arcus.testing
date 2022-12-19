using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Security
{
    [Trait("Category", "Unit")]
    public class SecretStoreBuilderExtensionsTests
    {
        [Fact]
        public void ConfigureSecretStore_WithEmptyInMemory_RegistersSecretProvider()
        {
            // Arrange
            var builder = new HostBuilder();

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory());

            // Assert
            using (IHost host = builder.Build())
            {
                Assert.NotNull(host.Services.GetService<ISecretProvider>());
            }
        }

        [Fact]
        public async Task ConfigureSecretStore_WithInMemorySecret_GetRawSecretAsyncSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                string actual = await provider.GetRawSecretAsync(secretName);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void ConfigureSecretStore_WithInMemorySecret_GetRawSecretSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                string actual = provider.GetRawSecret(secretName);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task ConfigureSecretStore_WithInMemorySecret_GetSecretAsyncSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                Secret actual = await provider.GetSecretAsync(secretName);
                Assert.Equal(expected, actual.Value);
            }
        }

        [Fact]
        public void ConfigureSecretStore_WithInMemorySecret_GetSecretSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                Secret actual = provider.GetSecret(secretName);
                Assert.Equal(expected, actual.Value);
            }
        }

        [Fact]
        public async Task ConfigureSecretStore_WithInMemorySecrets_GetRawSecretAsyncSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secrets));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                IEnumerable<Task<string>> getSecrets = secrets.Select(async secret => await provider.GetRawSecretAsync(secret.Key));
                string[] results = await Task.WhenAll(getSecrets);
                
                Assert.Equal(secrets.Count, results.Length);
                Assert.All(secrets.Values, secretValue => Assert.Contains(secretValue, results));
            }
        }

        [Fact]
        public void ConfigureSecretStore_WithInMemorySecrets_GetRawSecretSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secrets));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                string[] results = secrets.Select(secret => provider.GetRawSecret(secret.Key)).ToArray();
                
                Assert.Equal(secrets.Count, results.Length);
                Assert.All(secrets.Values, secretValue => Assert.Contains(secretValue, results));
            }
        }

        [Fact]
        public async Task ConfigureSecretStore_WithInMemorySecrets_GetSecretAsyncSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secrets));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                IEnumerable<Task<Secret>> getSecrets = secrets.Select(async secret => await provider.GetSecretAsync(secret.Key));
                Secret[] results = await Task.WhenAll(getSecrets);
                string[] secretValues = results.Select(result => result.Value).ToArray();
                
                Assert.Equal(secrets.Count, results.Length);
                Assert.All(secrets.Values, secretValue => Assert.Contains(secretValue, secretValues));
            }
        }

        [Fact]
        public void ConfigureSecretStore_WithInMemorySecrets_GetSecretSucceeds()
        {
            // Arrange
            var builder = new HostBuilder();
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };

            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secrets));

            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ISecretProvider>();
                Secret[] results = secrets.Select(secret => provider.GetSecret(secret.Key)).ToArray();
                string[] secretValues = results.Select(result => result.Value).ToArray();
                
                Assert.Equal(secrets.Count, results.Length);
                Assert.All(secrets.Values, secretValue => Assert.Contains(secretValue, secretValues));
            }
        }

        [Fact]
        public async Task ConfigureSecretStore_WithoutInMemoryCaching_FailsToAccessGetRawSecretWithCaching()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            
            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected));
            
            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ICachedSecretProvider>();
                await Assert.ThrowsAnyAsync<NotSupportedException>(
                    () => provider.GetRawSecretAsync(secretName, ignoreCache: false));
            }
        }
        
        [Fact]
        public async Task ConfigureSecretStore_WithoutInMemoryCaching_SucceedsToAccessGetRawSecretWithCaching()
        {
            // Arrange
            var builder = new HostBuilder();
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            
            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secretName, expected, CacheConfiguration.Default));
            
            // Assert
            using (IHost host = builder.Build())
            {
                var provider = host.Services.GetRequiredService<ICachedSecretProvider>();
                string actual = await provider.GetRawSecretAsync("MySecret", ignoreCache: false);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void ConfigureSecretStore_WithoutSecretName_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(secretName: null, secretValue: $"secret-{Guid.NewGuid()}");
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void ConfiguresSecretStore_WithoutSecrets_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) => stores.AddInMemory(secrets: null));
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }
        
        [Fact]
        public void ConfigureSecretStore_WithoutSecretNameWithCacheConfiguration_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(
                    secretName: null,
                    secretValue: $"secret-{Guid.NewGuid()}",
                    cacheConfiguration: CacheConfiguration.Default);
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void ConfiguresSecretStore_WithoutSecretsWithCacheConfiguration_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(secrets: null, cacheConfiguration: CacheConfiguration.Default);
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }
        
        [Fact]
        public void ConfigureSecretStore_WithSecretNameWithoutCacheConfiguration_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(
                    secretName: "MySecret", 
                    secretValue: $"secret-{Guid.NewGuid()}", 
                    cacheConfiguration: null);
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void ConfiguresSecretStore_WithSecretsWithoutCacheConfiguration_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(secrets: new Dictionary<string, string>
                {
                    ["MySecret"] = $"secret-{Guid.NewGuid()}"
                }, cacheConfiguration: null);
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void ConfigureSecretStore_WithoutCacheConfiguration_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureSecretStore((config, stores) =>
            {
                stores.AddInMemory(cacheConfiguration: null);
            });
            
            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }
    }
}
