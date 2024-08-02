namespace Arcus.Testing.Tests.Integration.Configuration
{
    public class ServicePrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal" /> class.
        /// </summary>
        public ServicePrincipal(string tenantId, string clientId, string clientSecret)
        {
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string TenantId { get; }

        public string ClientId { get; }
        
        public string ClientSecret { get; }
    }

    public static class TestConfigExtensions
    {
        public static ServicePrincipal GetServicePrincipal(this TestConfig config)
        {
            return new ServicePrincipal(
                config["Arcus:TenantId"],
                config["Arcus:ServicePrincipal:ClientId"],
                config["Arcus:ServicePrincipal:ClientSecret"]);
        }
    }
}
