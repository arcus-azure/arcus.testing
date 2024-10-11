namespace Arcus.Testing.Tests.Integration.Messaging.Configuration
{
    public class ServiceBusNamespace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusNamespace" /> class.
        /// </summary>
        public ServiceBusNamespace(string serviceBusNamespace)
        {
            HostName = $"{serviceBusNamespace}.servicebus.windows.net";
        }

        public string HostName { get; }
    }

    public static class ServiceBusNamespaceExtensions
    {
        public static ServiceBusNamespace GetServiceBus(this TestConfig config)
        {
            return new ServiceBusNamespace(
                config["Arcus:ServiceBus:Namespace"]);
        }
    }
}
