using System;

namespace Arcus.Testing.Tests.Integration.Storage.Configuration
{
    /// <summary>
    /// Represents the test configuration that is used to interact with the Azure Storage account.
    /// </summary>
    public class StorageAccount
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccount" /> class.
        /// </summary>
        /// <param name="name">The name of the Azure Storage account.</param>
        /// <param name="key">The account key of Azure Storage account.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> or the <paramref name="key"/> is blank.</exception>
        public StorageAccount(string name, string key)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Storage account name cannot be blank", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Storage account key cannot be blank", nameof(name));
            }

            Name = name;
            ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={name};AccountKey={key};EndpointSuffix=core.windows.net";
        }

        /// <summary>
        /// Gets the name of the Azure Storage account.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the connection string to the Azure Storage account.
        /// </summary>
        public string ConnectionString { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> related to Azure Storage test configuration.
    /// </summary>
    public static class TestConfigExtensions
    {
        /// <summary>
        /// Gets the Azure Storage account from the test configuration.
        /// </summary>
        public static StorageAccount GetStorageAccount(this TestConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new StorageAccount(
                config["Arcus:StorageAccount:Name"],
                config["Arcus:StorageAccount:Key"]);
        }
    }
}