using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Testing.Messaging.Pumps.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a set of extensions on the <see cref="IServiceCollection"/> to easily add the <see cref="TestServiceBusMessagePump"/>.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a test Azure Service Bus message pump to simulate received messages.
        /// </summary>
        /// <param name="services">The available registered services in the application.</param>
        /// <param name="configureProducer">The function to configure the message producer which will simulate messages on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="configureProducer"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddTestServiceBusMessagePump(
            this IServiceCollection services,
            Action<TestAzureServiceBusMessageProducer> configureProducer)
        {
            Guard.NotNull(services, nameof(services), "Requires a series of registered application services to add the test Azure Service Bus message pump");
            Guard.NotNull(configureProducer, nameof(configureProducer), "Requires a function to configure the message producer which will simulate messages on the message pump");

            return AddTestServiceBusMessagePump(services, configureProducer, configureOptions: null);
        }

        /// <summary>
        /// Adds a test Azure Service Bus message pump to simulate received messages.
        /// </summary>
        /// <param name="services">The available registered services in the application.</param>
        /// <param name="configureProducer">The function to configure the message producer which will simulate messages on the message pump.</param>
        /// <param name="configureOptions">The additional message routing options to configure the message router that will process the simulated messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="configureProducer"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddTestServiceBusMessagePump(
            this IServiceCollection services,
            Action<TestAzureServiceBusMessageProducer> configureProducer,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a series of registered application services to add the test Azure Service Bus message pump");
            Guard.NotNull(configureProducer, nameof(configureProducer), "Requires a function to configure the message producer which will simulate messages on the message pump");

            var producer = new TestAzureServiceBusMessageProducer();
            configureProducer(producer);

            return AddTestServiceBusMessagePump(services, producer, configureOptions);
        }

        /// <summary>
        /// Adds a test Azure Service Bus message pump to simulate received messages.
        /// </summary>
        /// <param name="services">The available registered services in the application.</param>
        /// <param name="messageProducer">The function to configure the message producer which will simulate messages on the message pump.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="messageProducer"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddTestServiceBusMessagePump(
            this IServiceCollection services,
            IAzureServiceBusMessageProducer messageProducer)
        {
            Guard.NotNull(services, nameof(services), "Requires a series of registered application services to add the test Azure Service Bus message pump");
            Guard.NotNull(messageProducer, nameof(messageProducer), "Requires a message producer instance to simulate messages on the message pump");

            return AddTestServiceBusMessagePump(services, messageProducer, configureOptions: null);
        }

        /// <summary>
        /// Adds a test Azure Service Bus message pump to simulate received messages.
        /// </summary>
        /// <param name="services">The available registered services in the application.</param>
        /// <param name="messageProducer">The function to configure the message producer which will simulate messages on the message pump.</param>
        /// <param name="configureOptions">The additional message routing options to configure the message router that will process the simulated messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="messageProducer"/> is <c>null</c>.</exception>
        public static ServiceBusMessageHandlerCollection AddTestServiceBusMessagePump(
            this IServiceCollection services,
            IAzureServiceBusMessageProducer messageProducer,
            Action<AzureServiceBusMessageRouterOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a series of registered application services to add the test Azure Service Bus message pump");
            Guard.NotNull(messageProducer, nameof(messageProducer), "Requires a message producer instance to simulate messages on the message pump");

            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting(configureOptions);
            services.AddHostedService(provider =>
            {
                var router = provider.GetRequiredService<IAzureServiceBusMessageRouter>();

                ILogger<TestServiceBusMessagePump> logger =
                    provider.GetService<ILogger<TestServiceBusMessagePump>>()
                    ?? NullLogger<TestServiceBusMessagePump>.Instance;

                return new TestServiceBusMessagePump(messageProducer, router, logger);
            });

            return collection;
        }
    }
}
