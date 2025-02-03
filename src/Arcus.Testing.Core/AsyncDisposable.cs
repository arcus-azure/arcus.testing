using System;
using System.Threading.Tasks;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents an asynchronous disposable function, implemented as an <see cref="IAsyncDisposable"/>.
    /// </summary>
    public class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<ValueTask> _disposeAsync;

        private AsyncDisposable(Func<ValueTask> disposeAsync)
        {
            _disposeAsync = disposeAsync ?? throw new ArgumentNullException(nameof(disposeAsync));
        }

        /// <summary>
        /// Creates an <see cref="AsyncDisposable"/> instance based on an existing synchronous <paramref name="disposable"/>.
        /// </summary>
        /// <param name="disposable">The synchronous disposable to create as an instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposable"/> is <c>null</c>.</exception>
        public static AsyncDisposable Create(IDisposable disposable)
        {
            if (disposable is null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            return Create(disposable.Dispose);
        }

        /// <summary>
        /// Creates an <see cref="AsyncDisposable"/> instance based on an existing synchronous <paramref name="dispose"/> operation.
        /// </summary>
        /// <param name="dispose">The synchronous operation to create as an instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dispose"/> is <c>null</c>.</exception>
        public static AsyncDisposable Create(Action dispose)
        {
            if (dispose is null)
            {
                throw new ArgumentNullException(nameof(dispose));
            }

            return Create(() =>
            {
                dispose();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Creates an <see cref="AsyncDisposable"/> instance based on an existing asynchronous <paramref name="disposeAsync"/> operation.
        /// </summary>
        /// <param name="disposeAsync">The asynchronous operation to create as an instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposeAsync"/> is <c>null</c>.</exception>
        public static AsyncDisposable Create(Func<Task> disposeAsync)
        {
            if (disposeAsync is null)
            {
                throw new ArgumentNullException(nameof(disposeAsync));
            }

            return new AsyncDisposable(async () => await disposeAsync());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _disposeAsync();
        }
    }
}