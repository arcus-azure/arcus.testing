using System;
using System.IO;
using System.Threading.Tasks;
using Bogus;
using Bogus.Extensions;
using Polly.Timeout;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Core
{
    public class PollTests : IAsyncLifetime
    {
        private readonly TimeSpan _100ms = TimeSpan.FromMilliseconds(100), _10ms = TimeSpan.FromMilliseconds(10);
        private readonly object _expectedResult = Bogus.PickRandom((object) Bogus.Random.Int(), Bogus.Random.AlphaNumeric(10));

        private static int Index;
        private static readonly Faker Bogus = new();

        [Fact]
        public async Task Poll_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await Poll.UntilAvailableAsync(AlwaysSucceedsAsync);
            await Poll.UntilAvailableAsync(SometimesSucceedsAsync, MinTimeFrame);
            await Poll.UntilAvailableAsync<InvalidOperationException>(AlwaysSucceedsAsync);
            await Poll.UntilAvailableAsync<TestPollException>(SometimesSucceedsAsync, MinTimeFrame);

            await Poll.Target(AlwaysSucceedsAsync);
            await Poll.Target(SometimesSucceedsAsync).MinTimeFrame();
            await Poll.Target<ArrayTypeMismatchException>(AlwaysSucceedsAsync);
            await Poll.Target<TestPollException>(SometimesSucceedsAsync).MinTimeFrame();

            await GetsResultAsync(() => Poll.UntilAvailableAsync(AlwaysSucceedsResultAsync));
            await GetsResultAsync(() => Poll.UntilAvailableAsync(SometimesSucceedsResultAsync, MinTimeFrame));
            await GetsResultAsync(() => Poll.UntilAvailableAsync<object, AggregateException>(AlwaysSucceedsResultAsync));
            await GetsResultAsync(() => Poll.UntilAvailableAsync<object, TestPollException>(SometimesSucceedsResultAsync, MinTimeFrame));

            await GetsResultAsync(async () => await Poll.Target(AlwaysSucceedsResultAsync));
            await GetsResultAsync(async () => await Poll.Target(SometimesSucceedsResultAsync).MinTimeFrame());
            await GetsResultAsync(async () => await Poll.Target<object, DllNotFoundException>(AlwaysSucceedsResultAsync));
            await GetsResultAsync(async () => await Poll.Target<object, TestPollException>(SometimesSucceedsResultAsync).MinTimeFrame());
        }

        [Fact]
        public async Task Poll_WithTargetRemainsUnavailableWithinTimeFrame_FailsWithDescription()
        {
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsAsync, MinTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsResultAsync, MinTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<TestPollException>(AlwaysFailsAsync, MinTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<object, TestPollException>(AlwaysFailsResultAsync, MinTimeFrame));

            await FailsByExceptionAsync(async () => await Poll.Target(AlwaysFailsAsync).MinTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target(AlwaysFailsResultAsync).Until(AlwaysTrue).MinTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<TestPollException>(AlwaysFailsAsync).MinTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<object, TestPollException>(AlwaysFailsResultAsync).Until(AlwaysTrue).MinTimeFrame());
        }

        [Fact]
        public async Task Poll_WithDiffExceptionThanUnavailableTarget_FailsDirectlyWithDescription()
        {
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<InvalidOperationException>(AlwaysFailsAsync, MinTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<object, FileNotFoundException>(AlwaysFailsResultAsync, MinTimeFrame));

            await FailsByExceptionAsync(async () => await Poll.Target<AggregateException>(AlwaysFailsAsync).MinTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<object, ApplicationException>(AlwaysFailsResultAsync).MinTimeFrame());
        }

        [Fact]
        public async Task Poll_WithNegativeUntilTargetPredicate_FailsWithDescription()
        {
            // Arrange
            string expected = Bogus.Lorem.Sentence();

            // Act / Assert
            await FailsByResultAsync(async () =>
                await Poll.Target(AlwaysSucceedsResultAsync)
                          .MinTimeFrame()
                          .Until(AlwaysTrue)
                          .Until(AlwaysFalse)
                          .Until(AlwaysTrue)
                          .FailWith(expected), errorParts: expected);
            
            await FailsByResultAsync(async () => 
                await Poll.Target<object, TestPollException>(AlwaysSucceedsResultAsync)
                          .MinTimeFrame()
                          .Until(AlwaysTrue)
                          .Until(AlwaysFalse)
                          .Until(AlwaysTrue)
                          .FailWith(expected), errorParts: expected);
        }

        [Fact]
        public async Task Poll_WithCustomFailureMessage_FailsWithGivenMessage()
        {
            // Arrange
            string expected = Bogus.Lorem.Sentence();

            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsAsync, WithMessage(expected)), expected);
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsResultAsync, WithMessage(expected)), expected);
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<InvalidOperationException>(AlwaysFailsAsync, WithMessage(expected)), expected);
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<object, AggregateException>(AlwaysFailsResultAsync, WithMessage(expected)), expected);
        }

        private Action<PollOptions> WithMessage(string message)
        {
            return options =>
            {
                options.FailureMessage = message;
                MinTimeFrame(options);
            };
        }

        private void MinTimeFrame(PollOptions options)
        {
            options.Timeout = _100ms;
            options.Interval = _10ms;
        }

        private static bool AlwaysTrue(object result) => true;
        private static bool AlwaysFalse(object result) => false;
        private static Task AlwaysFailsAsync() => throw new TestPollException();
        private static Task<object> AlwaysFailsResultAsync() => throw new TestPollException();
        private static Task AlwaysSucceedsAsync() => Task.CompletedTask;
        private Task<object> AlwaysSucceedsResultAsync() => Task.FromResult(_expectedResult);

        private static async Task SometimesSucceedsAsync()
        {
            if (++Index < 3)
            {
                throw new TestPollException("Sabotage polling!");
            }

            await Task.CompletedTask;
        }

        private async Task<object> SometimesSucceedsResultAsync()
        {
            await SometimesSucceedsAsync();
            return _expectedResult;
        }

        private async Task GetsResultAsync(Func<Task<object>> pollAsync)
        {
            object actualResult = await pollAsync();
            Assert.Equal(_expectedResult, actualResult);
        }

        private static async Task FailsByResultAsync(Func<Task<object>> pollAsync, params string[] errorParts)
        {
            TimeoutException exception = await ShouldFailAsync<TimeoutRejectedException>(pollAsync, errorParts);
            Assert.Contains("last result", exception.Message);
        }

        private static async Task FailsByExceptionAsync(Func<Task> pollAsync, params string[] errorParts)
        {
            await ShouldFailAsync<TestPollException>(pollAsync, errorParts);
        }

        private static async Task<TimeoutException> ShouldFailAsync<TException>(Func<Task> pollAsync, params string[] errorParts)
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(pollAsync);
            Assert.Contains(nameof(Poll), exception.Message);

            var parts = errorParts.Length == 0 ? new[] { "not succeed", "time frame" } : errorParts;
            Assert.All(parts, part => Assert.Contains(part, exception.Message));

            Assert.NotNull(exception.InnerException);
            Assert.Equal(typeof(TException), exception.InnerException.GetType());

            return exception;
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public Task DisposeAsync()
        {
            Index = 0;
            return Task.CompletedTask;
        }

        [Fact]
        public void Set_NegativeInterval_Fails()
        {
            // Arrange
            var options = new PollOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Interval = Bogus.Date.Timespan().Negate());
        }

        [Fact]
        public void Set_NegativeOrZeroTimeout_Fails()
        {
            // Arrange
            var options = new PollOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.Timeout = Bogus.Date.Timespan().Negate().OrDefault(Bogus));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Set_BlankFailureMessage_Fails(string blank)
        {
            // Arrange
            var options = new PollOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.FailureMessage = blank);
        }
    }

    public static class PollExtensions
    {
        public static Poll<TResult, TException> MinTimeFrame<TResult, TException>(
            this Poll<TResult, TException> poll)
            where TException : Exception
        {
            return poll.Every(TimeSpan.FromMilliseconds(10)).Timeout(TimeSpan.FromMilliseconds(100));
        }
    }

    [Serializable]
    public class TestPollException : Exception
    {
        public TestPollException() : base("Test poll exception") { }
        public TestPollException(string message) : base(message) { }
        public TestPollException(string message, Exception innerException) : base(message, innerException) { }
    }
}
