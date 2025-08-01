using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a configurable filter instance that selects a subset of <see cref="ServiceBusReceivedMessage"/>s on an Azure Service Bus queue or topic subscription
    /// (a.k.a. 'spy test fixture').
    /// </summary>
    public class ServiceBusMessageFilter
    {
        private readonly string _entityName, _subscriptionName;
        private readonly ServiceBusClient _client;
        private readonly Collection<Func<ServiceBusReceivedMessage, bool>> _predicates = [];

        private bool _fromDeadLetter;
        private int _maxMessages = 100;

        internal ServiceBusMessageFilter(string entityName, ServiceBusClient client)
        {
            ArgumentNullException.ThrowIfNull(entityName);
            ArgumentNullException.ThrowIfNull(client);

            _entityName = entityName;
            _client = client;
        }

        internal ServiceBusMessageFilter(string entityName, string subscriptionName, ServiceBusClient client)
        {
            ArgumentNullException.ThrowIfNull(entityName);
            ArgumentNullException.ThrowIfNull(subscriptionName);
            ArgumentNullException.ThrowIfNull(client);

            _entityName = entityName;
            _subscriptionName = subscriptionName;
            _client = client;
        }

        /// <summary>
        /// Configures the filter to only select a subset of messages that matches the given <paramref name="predicate"/>.
        /// Multiple calls gets aggregated.
        /// </summary>
        /// <param name="predicate">The custom predicate to match a message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="predicate"/> is <c>null</c>.</exception>
        public ServiceBusMessageFilter Where(Func<ServiceBusReceivedMessage, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            _predicates.Add(predicate);
            return this;
        }

        /// <summary>
        /// Configures the filter to peek from the dead-lettered sub-queue instead.
        /// </summary>
        public ServiceBusMessageFilter FromDeadLetter()
        {
            _fromDeadLetter = true;
            return this;
        }

        /// <summary>
        /// Configures the filter to return a maximum allowed number of messages to be peeked (default: 100).
        /// </summary>
        /// <param name="maxMessages">The maximum allowed messages to be peeked from the bus.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="maxMessages"/> is below one.</exception>
        public ServiceBusMessageFilter Take(int maxMessages)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxMessages, 1);

            _maxMessages = maxMessages;
            return this;
        }

        /// <summary>
        /// Determines whether the filtered <see cref="ServiceBusReceivedMessage"/> sequence contains any messages.
        /// </summary>
        /// <remarks>
        ///     Deferred messages are also included as messages are peeked.
        /// </remarks>
        public Task<bool> AnyAsync()
        {
            return AnyAsync(CancellationToken.None);
        }

        /// <summary>
        /// Determines whether the filtered <see cref="ServiceBusReceivedMessage"/> sequence contains any messages.
        /// </summary>
        /// <remarks>
        ///     Deferred messages are also included as messages are peeked.
        /// </remarks>
        /// <param name="cancellationToken">
        ///     The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.
        /// </param>
        public async Task<bool> AnyAsync(CancellationToken cancellationToken)
        {
            List<ServiceBusReceivedMessage> messages = await ToListAsync(cancellationToken).ConfigureAwait(false);
            return messages.Count > 0;
        }

        /// <summary>
        /// Gets the awaiter used to await the <see cref="ToListAsync()"/>.
        /// </summary>
        public TaskAwaiter<List<ServiceBusReceivedMessage>> GetAwaiter()
        {
            return ToListAsync().GetAwaiter();
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> from a filtered <see cref="ServiceBusReceivedMessage"/> collection
        /// </summary>
        /// <remarks>
        ///     Deferred messages are also included as messages are peeked.
        /// </remarks>
        public Task<List<ServiceBusReceivedMessage>> ToListAsync()
        {
            return ToListAsync(CancellationToken.None);
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> from a filtered <see cref="ServiceBusReceivedMessage"/> collection
        /// </summary>
        /// <remarks>
        ///     Deferred messages are also included as messages are peeked.
        /// </remarks>
        /// <param name="cancellationToken">
        ///     The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.
        /// </param>
        public async Task<List<ServiceBusReceivedMessage>> ToListAsync(CancellationToken cancellationToken)
        {
            var options = new ServiceBusReceiverOptions
            {
                SubQueue = _fromDeadLetter ? SubQueue.DeadLetter : SubQueue.None
            };

            ServiceBusReceiver receiver =
                _subscriptionName is null
                    ? _client.CreateReceiver(_entityName, options)
                    : _client.CreateReceiver(_entityName, _subscriptionName, options);

            await using (receiver.ConfigureAwait(false))
            {
                IReadOnlyList<ServiceBusReceivedMessage> messages =
                    await receiver.PeekMessagesAsync(_maxMessages, cancellationToken: cancellationToken)
                                  .ConfigureAwait(false);

                return messages.Where(msg => _predicates.All(predicate => predicate(msg))).ToList();
            }
        }
    }
}
