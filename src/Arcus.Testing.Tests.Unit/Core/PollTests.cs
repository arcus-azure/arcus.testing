using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Bogus;
using Polly.Timeout;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Core
{
    public class PollTests
    {
        private readonly TimeSpan _5s = TimeSpan.FromSeconds(5), _100ms = TimeSpan.FromMilliseconds(100);
        private readonly object _expectedResult = Bogus.PickRandom((object) Bogus.Random.Int(), Bogus.Random.AlphaNumeric(10));

        private static readonly Faker Bogus = new();

        [Fact]
        public async Task PollDirectAsync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await Poll.UntilAvailableAsync(AlwaysSucceedsAsync);
            await Poll.UntilAvailableAsync(SometimesSucceedsAsync, ReasonableTimeFrame);
            await Poll.UntilAvailableAsync<InvalidOperationException>(AlwaysSucceedsAsync);
            await Poll.UntilAvailableAsync<TestPollException>(SometimesSucceedsAsync, ReasonableTimeFrame);
        }

        [Fact]
        public async Task PollFluentAsync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await Poll.Target(AlwaysSucceedsAsync);
            await Poll.Target(SometimesSucceedsAsync).ReasonableTimeFrame();
            await Poll.Target<ArrayTypeMismatchException>(AlwaysSucceedsAsync);
            await Poll.Target<TestPollException>(SometimesSucceedsAsync).ReasonableTimeFrame();
        }

        [Fact]
        public async Task PollDirectResultAsync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await GetsResultAsync(() => Poll.UntilAvailableAsync(AlwaysSucceedsResultAsync));
            await GetsResultAsync(() => Poll.UntilAvailableAsync(SometimesSucceedsResultAsync, ReasonableTimeFrame));
            await GetsResultAsync(() => Poll.UntilAvailableAsync<object, AggregateException>(AlwaysSucceedsResultAsync));
            await GetsResultAsync(() => Poll.UntilAvailableAsync<object, TestPollException>(SometimesSucceedsResultAsync, ReasonableTimeFrame));
        }

        [Fact]
        public async Task PollFluentResultAsync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await GetsResultAsync(async () => await Poll.Target(AlwaysSucceedsResultAsync));
            await GetsResultAsync(async () => await Poll.Target(SometimesSucceedsResultAsync).ReasonableTimeFrame());
            await GetsResultAsync(async () => await Poll.Target<object, DllNotFoundException>(AlwaysSucceedsResultAsync));
            await GetsResultAsync(async () => await Poll.Target<object, TestPollException>(SometimesSucceedsResultAsync).ReasonableTimeFrame());
        }

        [Fact]
        public async Task PollFluentSync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await Poll.Target(AlwaysSucceeds);
            await Poll.Target(SometimesSucceeds).ReasonableTimeFrame();
            await Poll.Target<ArrayTypeMismatchException>(AlwaysSucceeds);
            await Poll.Target<TestPollException>(SometimesSucceeds).ReasonableTimeFrame();
        }

        [Fact]
        public async Task PollDirectSync_WithTargetAvailableWithinTimeFrame_SucceedsByContinuing()
        {
            await GetsResultAsync(async () => await Poll.Target(AlwaysSucceedsResult));
            await GetsResultAsync(async () => await Poll.Target(SometimesSucceedsResult).ReasonableTimeFrame());
            await GetsResultAsync(async () => await Poll.Target<object, DllNotFoundException>(AlwaysSucceedsResult));
            await GetsResultAsync(async () => await Poll.Target<object, TestPollException>(SometimesSucceedsResult).ReasonableTimeFrame());
        }

        [Fact]
        public async Task PollDirectly_WithTargetRemainsUnavailableWithinTimeFrame_FailsWithDescription()
        {
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsAsync, LowestTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync(AlwaysFailsResultAsync, LowestTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<TestPollException>(AlwaysFailsAsync, LowestTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<object, TestPollException>(AlwaysFailsResultAsync, LowestTimeFrame));
        }

        [Fact]
        public async Task PollFluent_WithTargetRemainsUnavailableWithinTimeFrame_FailsWithDescription()
        {
            await FailsByExceptionAsync(async () => await Poll.Target(AlwaysFailsAsync).LowestTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target(AlwaysFailsResultAsync).Until(AlwaysTrue).LowestTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<TestPollException>(AlwaysFailsAsync).LowestTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<object, TestPollException>(AlwaysFailsResultAsync).Until(AlwaysTrue).LowestTimeFrame());
        }

        [Fact]
        public async Task Poll_WithDiffExceptionThanUnavailableTarget_FailsDirectlyWithDescription()
        {
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<InvalidOperationException>(AlwaysFailsAsync, LowestTimeFrame));
            await FailsByExceptionAsync(() => Poll.UntilAvailableAsync<object, FileNotFoundException>(AlwaysFailsResultAsync, LowestTimeFrame));

            await FailsByExceptionAsync(async () => await Poll.Target<AggregateException>(AlwaysFailsAsync).LowestTimeFrame());
            await FailsByExceptionAsync(async () => await Poll.Target<object, ApplicationException>(AlwaysFailsResultAsync).LowestTimeFrame());
        }

        [Fact]
        public async Task PollFailure_WithoutResult_ShouldFail()
        {
            await Assert.ThrowsAsync<TimeoutException>(() => Poll.UntilAvailableAsync(async () => await AlwaysFailsAsync(), LowestTimeFrame));
        }

        [Fact]
        public async Task Poll_WithUntilPredicate_Succeeds()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromMilliseconds(100);
            TimeSpan interval = TimeSpan.FromMilliseconds(10);

            // Act
            await FailsByResultAsync(async () =>
                await Poll.Target(AlwaysSucceedsResult)
                          .Until(_ => false)
                          .Every(interval)
                          .Timeout(timeout));

            // Assert
            stopwatch.Stop();
            Assert.True(stopwatch.Elapsed >= interval + interval, $"stopwatch should at least run until two intervals: {stopwatch.Elapsed} >= {timeout}");
        }

        [Fact]
        public async Task Poll_WithNegativeUntilTargetPredicate_FailsWithDescription()
        {
            // Arrange
            string expected = Bogus.Lorem.Sentence();

            // Act / Assert
            await FailsByResultAsync(async () =>
                await Poll.Target(AlwaysSucceedsResultAsync)
                          .LowestTimeFrame()
                          .Until(AlwaysTrue)
                          .Until(AlwaysFalse)
                          .Until(AlwaysTrue)
                          .FailWith(expected), errorParts: expected);

            await FailsByResultAsync(async () =>
                await Poll.Target<object, TestPollException>(AlwaysSucceedsResultAsync)
                          .LowestTimeFrame()
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

        [Fact]
        public async Task Poll_WithNotMatchedExceptionFilter_DoesNotRunPolling()
        {
            // Arrange
            var watch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(3);

            // Act
            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await Poll.Target<AggregateException>(Task () => throw new AggregateException())
                          .When(_ => false)
                          .Timeout(timeout));

            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await Poll.UntilAvailableAsync<InvalidOperationException>(
                    Task () => throw new InvalidOperationException(),
                    options =>
                    {
                        options.Timeout = timeout;
                        options.AddExceptionFilter((InvalidOperationException _) => false);
                    }));

            // Assert
            watch.Stop();
            Assert.True(timeout > watch.Elapsed, $"elapsed: {watch.Elapsed}");
        }

        [Fact]
        public async Task Poll_WithMatchedExceptionFilter_DoesRunPolling()
        {
            // Arrange
            var watch = Stopwatch.StartNew();
            var message = Bogus.Lorem.Sentence();
            var timeout = TimeSpan.FromSeconds(3);

            // Act
            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await Poll.Target<int, InvalidOperationException>(Task<int> () => throw new InvalidOperationException(message))
                          .Timeout(timeout)
                          .When(ex => ex.Message == message)
                          .When(_ => false));

            // Assert
            watch.Stop();
            Assert.True(timeout <= watch.Elapsed, $"elapsed: {watch.Elapsed}");
        }

        private Action<PollOptions> WithMessage(string message)
        {
            return options =>
            {
                options.FailureMessage = message;
                LowestTimeFrame(options);
            };
        }

        private void ReasonableTimeFrame(PollOptions options)
        {
            options.Timeout = _5s;
            options.Interval = _100ms;
        }

        private void LowestTimeFrame(PollOptions options)
        {
            options.Timeout = _100ms;
            options.Interval = TimeSpan.FromMilliseconds(10);
        }

        private static bool AlwaysTrue(object result) => true;
        private static bool AlwaysFalse(object result) => false;
        private static Task AlwaysFailsAsync() => throw new TestPollException();
        private static void AlwaysSucceeds() { /*Nothing here: explicitly ignore*/ }
        private object AlwaysSucceedsResult() => _expectedResult;
        private static Task<object> AlwaysFailsResultAsync() => throw new TestPollException();
        private static Task AlwaysSucceedsAsync() => Task.CompletedTask;
        private Task<object> AlwaysSucceedsResultAsync() => Task.FromResult(_expectedResult);

        private static void SometimesSucceeds()
        {
            if (Bogus.PickRandom(false, false, false, false, true))
            {
                throw new TestPollException("Sabotage polling!");
            }
        }
        private static Task SometimesSucceedsAsync()
        {
            SometimesSucceeds();
            return Task.CompletedTask;
        }

        private object SometimesSucceedsResult()
        {
            SometimesSucceeds();
            return _expectedResult;
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

        [Fact]
        public async Task PollUntilAvailable_WithoutTarget_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Poll.UntilAvailableAsync<object>(getTargetWithResultAsync: null));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Poll.UntilAvailableAsync<object, AccessViolationException>(null));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Poll.UntilAvailableAsync(null));
            await Assert.ThrowsAnyAsync<ArgumentException>(() => Poll.UntilAvailableAsync<InvalidCastException>(null));

            Assert.ThrowsAny<ArgumentException>(() => Poll.Target<object>(getTargetWithoutResultSync: null));
            Assert.ThrowsAny<ArgumentException>(() => Poll.Target<object, AccessViolationException>(getTargetWithResultSync: null));
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
        public static Poll<TResult, TException> ReasonableTimeFrame<TResult, TException>(
            this Poll<TResult, TException> poll)
            where TException : Exception
        {
            return poll.Every(TimeSpan.FromMilliseconds(100)).Timeout(TimeSpan.FromSeconds(5));
        }

        public static Poll<TResult, TException> LowestTimeFrame<TResult, TException>(
            this Poll<TResult, TException> poll)
            where TException : Exception
        {
            return poll.Every(TimeSpan.FromMilliseconds(10)).Timeout(TimeSpan.FromSeconds(1));
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
