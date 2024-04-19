using System;
using System.Threading.Tasks;
using Bogus;

namespace Arcus.Testing.Tests.Unit.Core.Fixture
{
    /// <summary>
    /// Represents a (spy) stub that inspects the result of a disposable operation.
    /// </summary>
    internal interface ISpyDisposable
    {
        /// <summary>
        /// Gets the end-result of a disposable operation.
        /// </summary>
        DisposeResult DisposeResult { get; }
    }

    /// <summary>
    /// Represents an abstract disposable template for disposable instances.
    /// </summary>
    internal abstract class StubDisposableTemplate : ISpyDisposable
    {
        private readonly Exception _exception;
        protected static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StubDisposableTemplate" /> class.
        /// </summary>
        protected StubDisposableTemplate()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StubDisposableTemplate" /> class.
        /// </summary>
        protected StubDisposableTemplate(Exception exception)
        {
            _exception = exception;
        }

        /// <summary>
        /// Gets the end-result of a disposable operation.
        /// </summary>
        public DisposeResult DisposeResult { get; private set; } = DisposeResult.None;

        /// <summary>
        /// Simulate a dispose based on previously configured setup.
        /// </summary>
        protected void DisposeCore()
        {
            if (_exception != null)
            {
                DisposeResult = DisposeResult.Failure;
                throw _exception;
            }

            DisposeResult = DisposeResult.Disposed;
        }
    }

    /// <summary>
    /// Represents a stubbed version of a synchronous disposable instance.
    /// </summary>
    internal class StubDisposable : StubDisposableTemplate, IDisposable
    {
        private StubDisposable() { }
        private StubDisposable(Exception exception) : base(exception) { }

        /// <summary>
        /// Creates an <see cref="StubDisposable"/> instance that succeeds upon disposal.
        /// </summary>
        public static StubDisposable Success => new();

        /// <summary>
        /// Creates an <see cref="StubDisposable"/> instance that fails upon disposal.
        /// </summary>
        public static StubDisposable Failure => new(Bogus.System.Exception());

        /// <summary>
        /// Creates an <see cref="StubDisposable"/> instance that fails upon disposal.
        /// </summary>
        public static StubDisposable CreateFailure(Exception exception) => new(exception);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => DisposeCore();
    }

    /// <summary>
    /// Represents a stubbed version of an asynchronous disposable instance.
    /// </summary>
    internal class StubAsyncDisposable : StubDisposableTemplate, IAsyncDisposable
    {
        private StubAsyncDisposable() { }
        private StubAsyncDisposable(Exception exception) : base(exception) { }

        /// <summary>
        /// Creates an <see cref="StubAsyncDisposable"/> instance that succeeds upon disposal.
        /// </summary>
        public static StubAsyncDisposable Success => new();

        /// <summary>
        /// Creates an <see cref="StubAsyncDisposable"/> instance that fails upon disposal.
        /// </summary>
        public static StubAsyncDisposable Failure => new(Bogus.System.Exception());

        /// <summary>
        /// Creates an <see cref="StubAsyncDisposable"/> instance that fails upon disposal.
        /// </summary>
        public static StubAsyncDisposable CreateFailure(Exception exception) => new(exception ?? throw new ArgumentNullException(nameof(exception)));

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public ValueTask DisposeAsync()
        {
            DisposeCore();
            return ValueTask.CompletedTask;
        }
    }
}
