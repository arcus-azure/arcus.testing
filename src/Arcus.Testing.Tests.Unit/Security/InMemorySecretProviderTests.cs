using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Testing.Security.Providers.InMemory;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Security
{
    [Trait("Category", "Unit")]
    public class InMemorySecretProviderTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void Create_WithoutSecretName_Fails(string secretName)
        {
            Assert.ThrowsAny<ArgumentException>(() => new InMemorySecretProvider(secretName, "some secret value"));
        }

        [Fact]
        public void Create_WithoutSecrets_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new InMemorySecretProvider(secrets: null));
        }

        [Fact]
        public async Task CreateWithoutSecrets_GetsRawSecretAsync_ReturnsNull()
        {
            // Arrange
            var provider = new InMemorySecretProvider();
            
            // Act
            string secretValue = await provider.GetRawSecretAsync("MySecret");
            
            // Assert
            Assert.Null(secretValue);
        }

        [Fact]
        public void CreateWithoutSecrets_GetsRawSecret_ReturnsNull()
        {
            // Arrange
            var provider = new InMemorySecretProvider();
            
            // Act
            string secretValue = provider.GetRawSecret("MySecret");
            
            // Assert
            Assert.Null(secretValue);
        }
        
        [Fact]
        public async Task CreateWithoutSecrets_GetSecretAsync_ReturnsNull()
        {
            // Arrange
            var provider = new InMemorySecretProvider();
            
            // Act
            Secret secret = await provider.GetSecretAsync("MySecret");
            
            // Assert
            Assert.Null(secret);
        }

        [Fact]
        public void CreateWithoutSecrets_GetSecret_ReturnsNull()
        {
            // Arrange
            var provider = new InMemorySecretProvider();
            
            // Act
            Secret secret = provider.GetSecret("MySecret");
            
            // Assert
            Assert.Null(secret);
        }
        
        [Fact]
        public async Task CreateWithSecretValue_GetsRawSecret_EqualsInitializedSecretValue()
        {
            // Arrange
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            var provider = new InMemorySecretProvider(secretName, expected);
            
            // Act
            string actual = await provider.GetRawSecretAsync(secretName);
            
            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateWithSecretValue_GetsRawSecretAsync_EqualsInitializedSecretValue()
        {
            // Arrange
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            var provider = new InMemorySecretProvider(secretName, expected);
            
            // Act
            string actual = provider.GetRawSecret(secretName);
            
            // Assert
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public async Task CreateWithSecretValue_GetsSecretAsync_EqualsInitializedSecretValue()
        {
            // Arrange
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            var provider = new InMemorySecretProvider(secretName, expected);
            
            // Act
            Secret actual = await provider.GetSecretAsync(secretName);
            
            // Assert
            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void CreateWithSecretValue_GetsSecret_EqualsInitializedSecretValue()
        {
            // Arrange
            string secretName = "MySecret";
            string expected = $"secret-{Guid.NewGuid()}";
            var provider = new InMemorySecretProvider(secretName, expected);
            
            // Act
            Secret actual = provider.GetSecret(secretName);
            
            // Assert
            Assert.Equal(expected, actual.Value);
        }
        
        [Fact]
        public async Task CreateWithSecrets_GetsRawSecretAsync_EqualsInitializedSecretValue()
        {
            // Arrange
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };
            var provider = new InMemorySecretProvider(secrets);

            // Act
            IEnumerable<Task<string>> getSecrets = secrets.Select(secret => provider.GetRawSecretAsync(secret.Key));

            // Assert
            string[] results = await Task.WhenAll(getSecrets);
            Assert.Equal(secrets.Count, results.Length);
            Assert.All(secrets.Values, value => Assert.Contains(value, results));
        }

        [Fact]
        public void CreateWithSecrets_GetsRawSecret_EqualsInitializedSecretValue()
        {
            // Arrange
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };
            var provider = new InMemorySecretProvider(secrets);

            // Act
            string[] results = secrets.Select(secret => provider.GetRawSecret(secret.Key)).ToArray();

            // Assert
            Assert.Equal(secrets.Count, results.Length);
            Assert.All(secrets.Values, value => Assert.Contains(value, results));
        }
        
        [Fact]
        public async Task CreateWithSecrets_GetsSecretAsync_EqualsInitializedSecretValue()
        {
            // Arrange
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };
            var provider = new InMemorySecretProvider(secrets);

            // Act
            IEnumerable<Task<Secret>> getSecrets = secrets.Select(secret => provider.GetSecretAsync(secret.Key));

            // Assert
            Secret[] results = await Task.WhenAll(getSecrets);
            string[] values = results.Select(result => result.Value).ToArray();
            Assert.Equal(secrets.Count, results.Length);
            Assert.All(secrets.Values, value => Assert.Contains(value, values));
        }

        [Fact]
        public void CreateWithSecrets_GetsSecret_EqualsInitializedSecretValue()
        {
            // Arrange
            var secrets = new Dictionary<string, string>
            {
                ["MySecret-1"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-2"] = $"secret-{Guid.NewGuid()}",
                ["MySecret-3"] = $"secret-{Guid.NewGuid()}"
            };
            var provider = new InMemorySecretProvider(secrets);

            // Act
            Secret[] results = secrets.Select(secret => provider.GetSecret(secret.Key)).ToArray();

            // Assert
            string[] values = results.Select(result => result.Value).ToArray();
            Assert.Equal(secrets.Count, results.Length);
            Assert.All(secrets.Values, value => Assert.Contains(value, values));
        }
    }
}
