using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options on the <see cref="DisposableCollection"/>.
    /// </summary>
    public class DisposeOptions
    {
        private int _retryCount = 3;
        private TimeSpan _retryInterval = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or sets the amount of times a failed test fixture's disposal should be retried.
        /// (Default: 3 times)
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
                _retryCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the time interval between each failed test fixture's disposal retry.
        /// (Default: 3 seconds)
        /// </summary>
        public TimeSpan RetryInterval
        {
            get => _retryInterval;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _retryInterval = value;
            }
        }
    }

    /// <summary>
    /// <para>Represents a collection of <see cref="IAsyncDisposable"/> which are handled as a single disposable.</para>
    /// <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
    /// </summary>
    public sealed class DisposableCollection : IAsyncDisposable, IReadOnlyCollection<IAsyncDisposable>
    {
        private readonly Collection<IAsyncDisposable> _disposables = [];
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableCollection" /> class.
        /// </summary>
        /// <param name="logger">The logger instance to write exception information when the disposable fixture failed to dispose.</param>
        public DisposableCollection(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the available options to manipulate the dispose behavior of the collection.
        /// </summary>
        public DisposeOptions Options { get; } = new();

        /// <summary>
        /// Gets the number of disposable elements in the collection.
        /// </summary>
        /// <returns>The number of disposable elements in the collection.</returns>
        public int Count => _disposables.Count;

        /// <summary>
        /// Adds a <paramref name="disposable"/> to this collection which will get disposed when this collection gets disposed.
        /// </summary>
        /// <param name="disposable">The disposable instance to add to the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposable"/> is <c>null</c>.</exception>
        public void Add(IAsyncDisposable disposable)
        {
            ArgumentNullException.ThrowIfNull(disposable);
            _disposables.Add(disposable);
        }

        /// <summary>
        /// Adds a <paramref name="disposable"/> to this collection which will get disposed when this collection gets disposed.
        /// </summary>
        /// <param name="disposable">The disposable instance to add to the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposable"/> is <c>null</c>.</exception>
        public void Add(IDisposable disposable)
        {
            ArgumentNullException.ThrowIfNull(disposable);
            Add(AsyncDisposable.Create(disposable));
        }

        /// <summary>
        /// Adds a range of <paramref name="disposables"/> to this collection which will get disposed when this collection gets disposed.
        /// </summary>
        /// <param name="disposables">The disposable instances to add to the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposables"/> or any of its elements are <c>null</c>.</exception>
        public void AddRange(IEnumerable<IAsyncDisposable> disposables)
        {
            ArgumentNullException.ThrowIfNull(disposables);

            foreach (IAsyncDisposable disposable in disposables)
            {
                Add(disposable);
            }
        }

        /// <summary>
        /// Adds a range of <paramref name="disposables"/> to this collection which will get disposed when this collection gets disposed.
        /// </summary>
        /// <param name="disposables">The disposable instances to add to the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposables"/> or any of its elements are <c>null</c>.</exception>
        public void AddRange(IEnumerable<IDisposable> disposables)
        {
            ArgumentNullException.ThrowIfNull(disposables);

            foreach (IDisposable disposable in disposables)
            {
                Add(disposable);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IAsyncDisposable> GetEnumerator()
        {
            return _disposables.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            AsyncRetryPolicy policy =
                Policy.Handle<Exception>(ex =>
                      {
                          _logger.LogTearDownError(ex, ex.Message, Options.RetryInterval);
                          return true;
                      })
                      .WaitAndRetryAsync(Options.RetryCount, _ => Options.RetryInterval);

            var exceptions = new Collection<Exception>();
            foreach (IAsyncDisposable fixture in _disposables)
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        await fixture.DisposeAsync().ConfigureAwait(false);

                    }).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Should catch all exceptions to determine if the fixture teardown failed.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(
                    "[Test:Teardown] Some test fixtures failed to tear down correctly, please check the collected exceptions for more information",
                    exceptions);
            }
        }
    }

    internal static partial class DisposableILoggerExtensions
    {
        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "[Test:Teardown] Test fixture failed to be tear down: '{Message}', retrying in {RetryInterval:g}...")]
        internal static partial void LogTearDownError(this ILogger logger, Exception exception, string message, TimeSpan retryInterval);
    }
}
