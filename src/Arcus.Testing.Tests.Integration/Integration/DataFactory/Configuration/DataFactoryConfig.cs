using System;
using Azure.Core;

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
            string subscriptionId,
            string resourceGroupName,
            string resourceName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceGroupName);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

            Name = resourceName;
            ResourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DataFactory/factories/{resourceName}");
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
    /// Extensions on the <see cref="TestConfig"/> for more easier access to the <see cref="DataFactoryConfig"/>.
    /// </summary>
    public static class TestConfigExtensions
    {
        /// <summary>
        /// Loads the <see cref="DataFactoryConfig"/> instance from the current test <paramref name="config"/>.
        /// </summary>
        public static DataFactoryConfig GetDataFactory(this TestConfig config)
        {
            return new DataFactoryConfig(
                config["Arcus:SubscriptionId"],
                config["Arcus:ResourceGroup:Name"],
                config["Arcus:DataFactory:Name"]);
        }
    }
}