using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Testing.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents an <see cref="ServiceBusReceiver"/> for testing.
    /// </summary>
    public class TestServiceBusReceiver : ServiceBusReceiver
    {
        /// <summary>
        /// The path of the Service Bus entity that the receiver is connected to, specific to the
        /// Service Bus namespace that contains it.
        /// </summary>
        public override string EntityPath { get; } = "Arcus.Testing.EntityPath";

        /// <summary>
        /// The fully qualified Service Bus namespace that the receiver is associated with.  This is likely
        /// to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public override string FullyQualifiedNamespace { get; } = "Arcus.Testing.FullyQualifiedNamespace"; 

        /// <summary>
        /// Completes a <see cref="T:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage" />. This will delete the message from the service.
        /// </summary>
        /// <param name="message">The message to complete.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <remarks>
        /// This operation can only be performed on a message that was received by this receiver
        /// when <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiver.ReceiveMode" /> is set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusReceiveMode.PeekLock" />.
        /// </remarks>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the message has expired or the message has already been completed. This does not apply for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessageLockLost" /> in this case.
        /// </exception>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the session has expired or the message has already been completed. This only applies for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.SessionLockLost" /> in this case.
        /// </exception>
        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Abandons a <see cref="T:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage" />.This will make the message available again for immediate processing as the lock on the message held by the receiver will be released.
        /// </summary>
        /// <param name="message">The <see cref="T:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage" /> to abandon.</param>
        /// <param name="propertiesToModify">The properties of the message to modify while abandoning the message.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <remarks>
        /// Abandoning a message will increase the delivery count on the message.
        /// This operation can only be performed on messages that were received by this receiver
        /// when <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiver.ReceiveMode" /> is set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusReceiveMode.PeekLock" />.
        /// </remarks>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the message has expired or the message has already been completed. This does not apply for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessageLockLost" /> in this case.
        /// </exception>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the session has expired or the message has already been completed. This only applies for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.SessionLockLost" /> in this case.
        /// </exception>
        public override Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object> propertiesToModify = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        /// <summary>Moves a message to the dead-letter subqueue.</summary>
        /// <param name="message">The <see cref="T:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage" /> to dead-letter.</param>
        /// <param name="propertiesToModify">The properties of the message to modify while moving to subqueue.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <remarks>
        /// In order to receive a message from the dead-letter queue or transfer dead-letter queue,
        /// set the <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiverOptions.SubQueue" /> property to <see cref="F:Azure.Messaging.ServiceBus.SubQueue.DeadLetter" />
        /// or <see cref="F:Azure.Messaging.ServiceBus.SubQueue.TransferDeadLetter" /> when calling
        /// <see cref="M:Azure.Messaging.ServiceBus.ServiceBusClient.CreateReceiver(System.String,Azure.Messaging.ServiceBus.ServiceBusReceiverOptions)" /> or
        /// <see cref="M:Azure.Messaging.ServiceBus.ServiceBusClient.CreateReceiver(System.String,System.String,Azure.Messaging.ServiceBus.ServiceBusReceiverOptions)" />.
        /// This operation can only be performed when <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiver.ReceiveMode" /> is set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusReceiveMode.PeekLock" />.
        /// </remarks>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the message has expired or the message has already been completed. This does not apply for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessageLockLost" /> in this case.
        /// </exception>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the session has expired or the message has already been completed. This only applies for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.SessionLockLost" /> in this case.
        /// </exception>
        public override Task DeadLetterMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object> propertiesToModify = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        /// <summary>Moves a message to the dead-letter subqueue.</summary>
        /// <param name="message">The <see cref="T:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage" /> to dead-letter.</param>
        /// <param name="deadLetterReason">The reason for dead-lettering the message.</param>
        /// <param name="deadLetterErrorDescription">The error description for dead-lettering the message.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <remarks>
        /// In order to receive a message from the dead-letter queue or transfer dead-letter queue,
        /// set the <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiverOptions.SubQueue" /> property to <see cref="F:Azure.Messaging.ServiceBus.SubQueue.DeadLetter" />
        /// or <see cref="F:Azure.Messaging.ServiceBus.SubQueue.TransferDeadLetter" /> when calling
        /// <see cref="M:Azure.Messaging.ServiceBus.ServiceBusClient.CreateReceiver(System.String,Azure.Messaging.ServiceBus.ServiceBusReceiverOptions)" /> or
        /// <see cref="M:Azure.Messaging.ServiceBus.ServiceBusClient.CreateReceiver(System.String,System.String,Azure.Messaging.ServiceBus.ServiceBusReceiverOptions)" />.
        /// This operation can only be performed when <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceiver.ReceiveMode" /> is set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusReceiveMode.PeekLock" />.
        /// </remarks>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the message has expired or the message has already been completed. This does not apply for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessageLockLost" /> in this case.
        /// </exception>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The lock for the session has expired or the message has already been completed. This only applies for session-enabled
        ///   entities.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.SessionLockLost" /> in this case.
        /// </exception>
        public override Task DeadLetterMessageAsync(
            ServiceBusReceivedMessage message,
            string deadLetterReason,
            string deadLetterErrorDescription = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}
