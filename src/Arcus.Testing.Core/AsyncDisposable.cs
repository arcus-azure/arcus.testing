using System;
using System.Threading.Tasks;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents an asynchronous disposable function, implemented as an <see cref="IAsyncDisposable"/>.
    /// </summary>
    public class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<Task> _disposeAsync;
        private bool _isDisposed;

        private AsyncDisposable(Func<Task> disposeAsync)
        {
            ArgumentNullException.ThrowIfNull(disposeAsync);
            _disposeAsync = disposeAsync;
        }

        /// <summary>
        /// Creates an <see cref="AsyncDisposable"/> instance based on an existing synchronous <paramref name="disposable"/>.
        /// </summary>
        /// <param name="disposable">The synchronous disposable to create as an instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="disposable"/> is <c>null</c>.</exception>
        public static AsyncDisposable Create(IDisposable disposable)
        {
            ArgumentNullException.ThrowIfNull(disposable);
            return Create(disposable.Dispose);
        }

        /// <summary>
        /// Creates an <see cref="AsyncDisposable"/> instance based on an existing synchronous <paramref name="dispose"/> operation.
        /// </summary>
        /// <param name="dispose">The synchronous operation to create as an instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dispose"/> is <c>null</c>.</exception>
        public static AsyncDisposable Create(Action dispose)
        {
            ArgumentNullException.ThrowIfNull(dispose);

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
            ArgumentNullException.ThrowIfNull(disposeAsync);
            return new AsyncDisposable(disposeAsync);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            await _disposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
