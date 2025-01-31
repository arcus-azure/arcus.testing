namespace Arcus.Testing.Tests.Integration.Configuration
{
    /// <summary>
    /// Represents the environment on Azure where a set of test resources are located.
    /// </summary>
    public class AzureEnvironment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEnvironment" /> class.
        /// </summary>
        public AzureEnvironment(
            string subscriptionId,
            string resourceGroupName)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroupName;
        }

        /// <summary>
        /// Gets the subscription ID for the set of test resources.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the resource group name for the set of test resources.
        /// </summary>
        public string ResourceGroupName { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> for more test-friendly interaction.
    /// </summary>
    public static partial class TestConfigExtensions
    {
        /// <summary>
        /// Loads the <see cref="AzureEnvironment"/> from the current test <paramref name="config"/>.
        /// </summary>
        public static AzureEnvironment GetAzureEnvironment(this TestConfig config)
        {
            return new AzureEnvironment(
                config["Arcus:SubscriptionId"],
                config["Arcus:ResourceGroup:Name"]);
        }
    }
}
