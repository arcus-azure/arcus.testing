using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.Testing.Tests.Unit.Core.Fixture;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Core
{
    public enum DisposeResult { None, Disposed, Failure }

    public class DisposableCollectionTests
    {
        private readonly ILogger _logger;
        private static readonly Faker Bogus = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableCollectionTests" /> class.
        /// </summary>
        public DisposableCollectionTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task Create_WithDisposablesContainingFailures_DisposesAllSuccessfulOnes()
        {
            // Arrange
            DisposableCollection collection = CreateCollection();
            ISpyDisposable[] failure = CreateFailureDisposables();
            ISpyDisposable[] success = CreateSuccessDisposables();
            Assert.All(Bogus.Random.Shuffle(failure.Concat(success)), d => AddDisposable(collection, d));

            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<AggregateException>(
                async () => await collection.DisposeAsync());

            Assert.All(success, d => Assert.Equal(DisposeResult.Disposed, d.DisposeResult));
            Assert.Equal(failure.Length, exception.InnerExceptions.Count);
        }

        [Fact]
        public async Task Create_WithSingleFailure_FailsWithSameException()
        {
            // Arrange
            DisposableCollection collection = CreateCollection();

            Exception exception = Bogus.System.Exception();
            AddDisposable(collection, Bogus.Random.Bool()
                ? StubAsyncDisposable.CreateFailure(exception)
                : StubDisposable.CreateFailure(exception));

            ISpyDisposable[] success = CreateSuccessDisposables();
            Assert.All(success, d => AddDisposable(collection, d));

            // Act / Assert
            await Assert.ThrowsAsync(exception.GetType(), async () => await collection.DisposeAsync());
            Assert.All(success, d => Assert.Equal(DisposeResult.Disposed, d.DisposeResult));
        }

        private static ISpyDisposable[] CreateSuccessDisposables()
        {
            return Bogus.Make<ISpyDisposable>(
                Bogus.Random.Int(5, 10),
                () => Bogus.Random.Bool() ? StubAsyncDisposable.Success : StubDisposable.Success).ToArray();
        }

        private static ISpyDisposable[] CreateFailureDisposables()
        {
            return Bogus.Make<ISpyDisposable>(
                Bogus.Random.Int(2, 3),
                () => Bogus.Random.Bool() ? StubAsyncDisposable.Failure : StubDisposable.Failure).ToArray();
        }

        private static void AddDisposable(DisposableCollection collection, ISpyDisposable disposable)
        {
            switch (disposable)
            {
                case IAsyncDisposable d:
                    collection.Add(d);
                    break;

                case IDisposable d:
                    collection.Add(d);
                    break;
            }
        }

        [Fact]
        public async Task Create_WithoutItems_Succeeds()
        {
            await using DisposableCollection disposables = CreateCollection();
            Assert.NotNull(disposables);
        }

        [Fact]
        public void Configure_WithNegativeRetryCount_Fails()
        {
            // Arrange
            DisposableCollection collection = CreateCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => collection.Options.RetryCount = Bogus.Random.Int(max: 0));
        }

        [Fact]
        public void Configure_WithNegativeRetryInterval_Fails()
        {
            // Arrange
            DisposableCollection collection = CreateCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => collection.Options.RetryInterval = Bogus.Date.Timespan().Negate());
        }

        private DisposableCollection CreateCollection()
        {
            var disposables = new DisposableCollection(_logger);

            disposables.Options.RetryCount = Bogus.Random.Int(1, 3);
            disposables.Options.RetryInterval = TimeSpan.FromMilliseconds(Bogus.Random.Int(100, 300));

            return disposables;
        }
    }
}
