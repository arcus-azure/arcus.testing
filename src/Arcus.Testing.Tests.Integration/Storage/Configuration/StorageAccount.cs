namespace Arcus.Testing.Tests.Integration.Storage.Configuration
{
    public class StorageAccount
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccount" /> class.
        /// </summary>
        public StorageAccount(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public static class TestConfigExtensions
    {
        public static StorageAccount GetStorageAccount(this TestConfig config)
        {
            return new StorageAccount(config["Arcus:StorageAccount:Name"]);
        }
    }
}
