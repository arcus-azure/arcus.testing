using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents an <see cref="IHostedService"/> that acts as a <see cref="MessagePump"/> for <see cref="ServiceBusReceivedMessage"/> messages.
    /// </summary>
    public class TestServiceBusMessagePump : IHostedService
    {
        private readonly IAzureServiceBusMessageProducer _messageProducer;
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly ILogger<TestServiceBusMessagePump> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessagePump" /> class.
        /// </summary>
        /// <param name="messageProducer">The producer instance to simulate received messages on the message pump.</param>
        /// <param name="messageRouter">The router instance to process the simulated received messages on the message pump.</param>
        /// <param name="logger">The logger instance to write diagnostic information during the message simulation on the message pump.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageProducer"/>, <paramref name="messageRouter"/>, or the <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public TestServiceBusMessagePump(
            IAzureServiceBusMessageProducer messageProducer,
            IAzureServiceBusMessageRouter messageRouter,
            ILogger<TestServiceBusMessagePump> logger)
        {
            Guard.NotNull(messageProducer, nameof(messageProducer), "Requires an Azure Service Bus message producer to simulate received messages on the message pump");
            Guard.NotNull(messageRouter, nameof(messageRouter), "Requires an Azure Service Bus message router to process the simulated received messages on the message pump");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic information during the message simulation on the message pump");

            _messageProducer = messageProducer;
            _messageRouter = messageRouter;
            _logger = logger;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Started test Azure Service Bus message pump");

            var receiver = new TestServiceBusReceiver();
            ServiceBusReceivedMessage[] messages = await _messageProducer.ProduceMessagesAsync();

            foreach (ServiceBusReceivedMessage message in messages)
            {
                try
                {
                    AzureServiceBusMessageContext context = message.GetMessageContext(jobId: Guid.NewGuid().ToString());
                    MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
                    await _messageRouter.RouteMessageAsync(receiver, message, context, correlationInfo, cancellationToken);
                }
                catch (Exception exception)
                {
                    var bodyString = message.Body.ToString();
                    var  propertiesDescription = $"[{string.Join(", ", message.ApplicationProperties.Select(prop => $"{prop.Key}={prop.Value}"))}]";
                    _logger.LogCritical(exception, "Failed to route test Azure Service Bus message {MessageId} (Body: {Body}, Properties: {Properties})", message.MessageId, bodyString, propertiesDescription);
                }
            }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopped test Azure Service Bus message pump");
            return Task.CompletedTask;
        }
    }
}