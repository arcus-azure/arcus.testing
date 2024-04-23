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
        public static AsyncDisposable Create(Action dispose)
        {
            if (dispose is null)
            {
                throw new ArgumentNullException(nameof(dispose));
            }

            return new AsyncDisposable(() =>
            {
                dispose();
                return ValueTask.CompletedTask;
            });
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
