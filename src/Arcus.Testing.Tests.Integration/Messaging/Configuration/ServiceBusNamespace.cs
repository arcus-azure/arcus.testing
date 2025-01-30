namespace Arcus.Testing.Tests.Integration.Messaging.Configuration
{
    /// <summary>
    /// Represents a test configuration subsection related to an Azure Service bus namespace.
    /// </summary>
    public class ServiceBusNamespace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusNamespace" /> class.
        /// </summary>
        public ServiceBusNamespace(string serviceBusNamespace)
        {
            HostName = $"{serviceBusNamespace}.servicebus.windows.net";
        }

        /// <summary>
        /// Gets the service URI for the Azure Service bus namespace.
        /// </summary>
        public string HostName { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TestConfig"/> for test-friendly retrieval of Azure Service bus-related subsections.
    /// </summary>
    public static class ServiceBusNamespaceExtensions
    {
        /// <summary>
        /// Loads the <see cref="ServiceBusNamespace"/> from the current test <paramref name="config"/>.
        /// </summary>
        public static ServiceBusNamespace GetServiceBus(this TestConfig config)
        {
            return new ServiceBusNamespace(
                config["Arcus:ServiceBus:Namespace"]);
        }
    }
}
