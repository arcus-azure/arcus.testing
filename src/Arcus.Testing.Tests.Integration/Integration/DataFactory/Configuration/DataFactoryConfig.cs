using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Azure.Core;
using Azure.ResourceManager.DataFactory;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    /// <summary>
    /// Represents an Azure DataFactory resource in the test configuration.
    /// </summary>
    public class DataFactoryConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFactoryConfig" /> class.
        /// </summary>
        public DataFactoryConfig(
            string factoryName,
            ResourceIdentifier factoryResourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(factoryName);
            ArgumentNullException.ThrowIfNull(factoryResourceId);

            Name = factoryName;
            ResourceId = factoryResourceId;
        }

        /// <summary>
        /// Gets the resource name of the Azure DataFactory resource.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the resource ID of the Azure DataFactory resource.
        /// </summary>
        public ResourceIdentifier ResourceId { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> for easier access to the <see cref="DataFactoryConfig"/>.
    /// </summary>
    public static class TestConfigExtensions
    {
        /// <summary>
        /// Loads the <see cref="DataFactoryConfig"/> instance from the current test <paramref name="config"/>.
        /// </summary>
        public static DataFactoryConfig GetDataFactory(this TestConfig config)
        {
            AzureEnvironment env = config.GetAzureEnvironment();

            string factoryName = config["Arcus:DataFactory:Name"];
            ResourceIdentifier factoryResourceId =
                DataFactoryResource.CreateResourceIdentifier(env.SubscriptionId, env.ResourceGroupName, factoryName);

            return new DataFactoryConfig(
                factoryName,
                factoryResourceId);
        }
    }
}