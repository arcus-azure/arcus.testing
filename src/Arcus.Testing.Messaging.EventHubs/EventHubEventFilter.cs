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
        /// 
        /// </summary>
        /// <param name="partitionId">The identifier of the Event Hub partition from which events will be received.</param>
        /// <param name="startingPosition">The position within the partition where the consumer should begin reading events.</param>
        /// <returns></returns>
        public EventHubEventFilter FromPartition(string partitionId, EventPosition startingPosition)
        {
            _partitionId = partitionId;
            _startingPosition = startingPosition;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public EventHubEventFilter Where(Func<PartitionEvent, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            _predicates.Add(predicate);

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public EventHubEventFilter ReadWith(Action<ReadEventOptions> configureOptions)
        {
            configureOptions(_options);
            return this;
        }

        /// <summary>
        /// Gets the awaiter used to await the <see cref="ToListAsync()"/>.
        /// </summary>
        public TaskAwaiter<List<PartitionEvent>> GetAwaiter()
        {
            return ToListAsync().GetAwaiter();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AnyAsync()
        {
            return await AnyAsync(CancellationToken.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> AnyAsync(CancellationToken cancellationToken)
        {
            List<PartitionEvent> events = await ToListAsync(cancellationToken);
            return events.Any();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<PartitionEvent>> ToListAsync()
        {
            return await ToListAsync(CancellationToken.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<PartitionEvent>> ToListAsync(CancellationToken cancellationToken)
        {
            IAsyncEnumerable<PartitionEvent> reading =
                _partitionId is null
                    ? _client.ReadEventsAsync(_options, cancellationToken)
                    : _client.ReadEventsFromPartitionAsync(_partitionId, _startingPosition, _options, cancellationToken);

            var events = new List<PartitionEvent>();
            await foreach (PartitionEvent ev in reading)
            {
                if (!ev.Equals(default) && _predicates.All(predicate => predicate(ev)))
                {
                    events.Add(ev);
                }
            }

            return events;
        }
    }
}
