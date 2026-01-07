using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a configurable filter instance that selects a subset of <see cref="PartitionEvent"/>s on an Azure EventHubs hub
    /// (a.k.a. 'spy test fixture').
    /// </summary>
    public class EventHubEventFilter
    {
        private readonly EventHubConsumerClient _client;
        private readonly ReadEventOptions _options = new() { MaximumWaitTime = TimeSpan.FromMinutes(1) };
        private readonly Collection<Func<PartitionEvent, bool>> _predicates = new();

        private string _partitionId;
        private EventPosition _startingPosition;

        internal EventHubEventFilter(EventHubConsumerClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            _client = client;
        }

        /// <summary>
        /// Indicate that only events from a given <paramref name="partitionId"/> should be searched for.
        /// </summary>
        /// <param name="partitionId">The identifier of the Event Hub partition from which events will be received.</param>
        /// <param name="startingPosition">The position within the partition where the consumer should begin reading events.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="partitionId"/> is blank.</exception>
        public EventHubEventFilter FromPartition(string partitionId, EventPosition startingPosition)
        {
            if (string.IsNullOrWhiteSpace(partitionId))
            {
                throw new ArgumentException("Requires a non-blank partition ID to search for events on an Azure Event Hub", nameof(partitionId));
            }

            _partitionId = partitionId;
            _startingPosition = startingPosition;
            return this;
        }

        /// <summary>
        /// Adds a <paramref name="predicate"/> to which the searched for events should match against.
        /// </summary>
        /// <param name="predicate">The custom filter function to select a subset of events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="predicate"/> is <c>null</c>.</exception>>
        public EventHubEventFilter Where(Func<PartitionEvent, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _predicates.Add(predicate);

            return this;
        }

        /// <summary>
        ///   <para>Configures the <see cref="ReadEventOptions"/> that will be associated with the event search operation.</para>
        ///   <para>Use for example the <see cref="ReadEventOptions.MaximumWaitTime"/> to shortcut the event searching early:</para>
        ///   <example>
        ///     <code>
        ///       .ReadWith(options =>
        ///       {
        ///           options.MaximumWaitTime = TimeSpan.FromSeconds(10);
        ///       })
        ///     </code>
        ///   </example>
        ///   <para>Or to change the <see cref="ReadEventOptions.OwnerLevel"/> when multiple event consumers are involved:</para>
        ///   <example>
        ///     <code>
        ///       .ReadWith(options =>
        ///       {
        ///           options.OwnerLevel = 10;
        ///       })
        ///     </code>
        ///   </example>
        /// </summary>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public EventHubEventFilter ReadWith(Action<ReadEventOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            configureOptions(_options);

            return this;
        }

        /// <summary>
        /// Gets the awaiter used to await the <see cref="ToListAsync()"/>.
        /// </summary>
        public TaskAwaiter<IReadOnlyList<PartitionEvent>> GetAwaiter()
        {
            return ToListAsync().GetAwaiter();
        }

        /// <summary>
        /// Determines whether the configured Azure Event Hub contains any matching events.
        /// </summary>
        /// <returns>
        ///     <see langword="true" /> if any events are found that matches the previously configured predicates; otherwise, <see langword="false" />.
        /// </returns>
        public async Task<bool> AnyAsync()
        {
            return await AnyAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines whether the configured Azure Event Hub contains any matching events.
        /// </summary>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>
        ///     <see langword="true" /> if any events are found that matches the previously configured predicates; otherwise, <see langword="false" />.
        /// </returns>
        public async Task<bool> AnyAsync(CancellationToken cancellationToken)
        {
            List<PartitionEvent> events = await ToListAsync(cancellationToken).ConfigureAwait(false);
            return events.Count > 0;
        }

        /// <summary>
        /// Collects all events currently matching on the configured Azure Event Hub into a <see cref="List{T}"/>.
        /// </summary>
        public async Task<IReadOnlyList<PartitionEvent>> ToListAsync()
        {
            return await ToListAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Collects all events currently matching on the configured Azure Event Hub into a <see cref="List{T}"/>.
        /// </summary>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        public async Task<IReadOnlyList<PartitionEvent>> ToListAsync(CancellationToken cancellationToken)
        {
            IAsyncEnumerable<PartitionEvent> reading =
                _partitionId is null
                    ? _client.ReadEventsAsync(_options, cancellationToken)
                    : _client.ReadEventsFromPartitionAsync(_partitionId, _startingPosition, _options, cancellationToken);

            var events = new Collection<PartitionEvent>();
            await foreach (PartitionEvent ev in reading.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                if (ev.Data is null)
                {
                    return events.ToList();
                }

                if (_predicates.All(predicate => predicate(ev)))
                {
                    events.Add(ev);
                }
            }

            return events.ToList();
        }
    }
}
