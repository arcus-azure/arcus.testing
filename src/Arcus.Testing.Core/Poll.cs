﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a polling operation that will run until the target is available.
    /// </summary>
    /// <typeparam name="TException">The type of exception that is expected to be thrown by the target.</typeparam>
    public class Poll<TException> : Poll<int, TException> where TException : Exception
    {
        internal Poll(Func<Task> getTargetWithoutResultAsync, PollOptions options)
            : base(async () => { await getTargetWithoutResultAsync().ConfigureAwait(false); return 0; }, options)
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultAsync);
        }
    }

    /// <summary>
    /// Represents a polling operation that will run until the target is available.
    /// </summary>
    /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
    /// <typeparam name="TException">The type of exception that is expected to be thrown by the target.</typeparam>
    public class Poll<TResult, TException> where TException : Exception
    {
        private readonly Func<Task<TResult>> _getTargetWithResultAsync;
        private readonly PollOptions _options;
        private readonly Collection<Func<TResult, bool>> _untilTargets = [];

        internal Poll(Func<Task<TResult>> getTargetWithResultAsync, PollOptions options)
        {
            ArgumentNullException.ThrowIfNull(getTargetWithResultAsync);
            ArgumentNullException.ThrowIfNull(options);

            _getTargetWithResultAsync = getTargetWithResultAsync;
            _options = options;
        }

        /// <summary>
        /// Adds a predicate to determine when the polling operation should stop.
        /// </summary>
        /// <remarks>This operation can be called multiple times, until filters will be aggregated.</remarks>
        /// <param name="untilTarget">
        ///     The custom function to determine the state of the current target, returns [false] when the target is unavailable; [true] otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="untilTarget"/> is <c>null</c>.</exception>
        public Poll<TResult, TException> Until(Func<TResult, bool> untilTarget)
        {
            ArgumentNullException.ThrowIfNull(untilTarget);
            _untilTargets.Add(untilTarget);

            return this;
        }

        /// <summary>
        /// Sets the interval between each poll operation.
        /// </summary>
        /// <param name="interval">The interval between each polling operation to the target.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="interval"/>"> represents a negative time frame.</exception>
        /// Same as <seealso cref="PollOptions.Interval"/>
        public Poll<TResult, TException> Every(TimeSpan interval)
        {
            _options.Interval = interval;
            return this;
        }

        /// <summary>
        /// Sets the time frame in which the polling operation has to succeed.
        /// </summary>
        /// <param name="timeout">The time frame in which the entire polling should succeed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/>"> represents a negative or zero time frame.</exception>
        /// Same as <seealso cref="PollOptions.Timeout"/>
        public Poll<TResult, TException> Timeout(TimeSpan timeout)
        {
            _options.Timeout = timeout;
            return this;
        }

        /// <summary>
        ///   <para>
        ///     Adds a filter to the polling operation to only run the polling operation
        ///     when the <typeparamref name="TException"/> represents a specific certain failure.
        ///   </para>
        ///   <para>Usually being used to describe 'transient' exceptions.</para>
        ///   <example>
        ///     <code>
        ///       Poll.Target(() => ...)
        ///           .When((RequestFailedException ex) => ex.StatusCode == 429);
        ///     </code>
        ///   </example>
        /// </summary>
        /// <param name="exceptionFilter">The custom filter to only select a subset of the given exceptions that needs to be polled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exceptionFilter"/> is <c>null</c>.</exception>
        /// Same as <seealso cref="PollOptions.AddExceptionFilter{TException}"/>
        public Poll<TResult, TException> When(Func<TException, bool> exceptionFilter)
        {
            ArgumentNullException.ThrowIfNull(exceptionFilter);
            _options.AddExceptionFilter(exceptionFilter);

            return this;
        }

        /// <summary>
        /// Sets the message that describes the failure of the polling operation.
        /// </summary>
        /// <param name="errorMessage">
        ///     The message that will be used as the final failure message of the <see cref="TimeoutException"/>
        ///     when the polling operation fails to succeed within the configured time frame.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="errorMessage"/> is blank.</exception>
        /// Same as <seealso cref="PollOptions.FailureMessage"/>
        public Poll<TResult, TException> FailWith(string errorMessage)
        {
            _options.FailureMessage = errorMessage;
            return this;
        }

        /// <summary>
        /// Gets the awaiter used to await this <see cref="Poll{TResult,TException}"/>.
        /// </summary>
        public TaskAwaiter<TResult> GetAwaiter()
        {
            return StartAsync().GetAwaiter();
        }

        /// <summary>
        /// Starts the polling operation with the previously configured options.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when the target remains unavailable after within the configured time frame.</exception>
        public async Task<TResult> StartAsync()
        {
            var exceptions = new Collection<Exception>();
            var results = new Collection<TResult>();

            AsyncRetryPolicy<TResult> retryPolicy =
                Policy.HandleResult((TResult r) =>
                      {
                          results.Add(r);
                          return !_untilTargets.All(untilTarget => untilTarget(r));

                      }).Or<TException>(ex =>
                      {
                          exceptions.Add(ex);
                          return _options.IsMatch(ex);
                      })
                      .WaitAndRetryForeverAsync(_ => _options.Interval);

            PolicyResult<TResult> target =
                await Policy.TimeoutAsync(_options.Timeout)
                            .WrapAsync(retryPolicy)
                            .ExecuteAndCaptureAsync(_getTargetWithResultAsync)
                            .ConfigureAwait(false);

            if (target.Outcome is OutcomeType.Failure)
            {
                string resultDescription = results.Count > 0 ? $" (last result: {results[^1]})" : string.Empty;
                throw new TimeoutException(_options.FailureMessage + resultDescription, exceptions.LastOrDefault() ?? target.FinalException);
            }

            return target.Result;
        }
    }

    /// <summary>
    /// <para>Represents the available user-configurable options for the <see cref="Poll"/> operation.</para>
    /// <para>See: <a href="https://testing.arcus-azure.net/features/core" /> for more information.</para>
    /// </summary>
    public class PollOptions
    {
        private TimeSpan _interval = TimeSpan.FromSeconds(1);
        private TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private string _failureMessage = "operation did not succeed within the given time frame";
        private readonly Collection<Func<Exception, bool>> _exceptionFilters = [];

        /// <summary>
        /// Gets the default set of polling options.
        /// </summary>
        internal static PollOptions Default => new();

        /// <summary>
        /// Gets or sets the interval between each poll operation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/>"> represents a negative time frame.</exception>
        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
                _interval = value;
            }
        }

        /// <summary>
        /// Gets or sets the time frame in which the polling operation has to succeed.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/>"> represents a negative time frame.</exception>
        public TimeSpan Timeout
        {
            get => _timeout;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the message that describes the failure of the polling operation.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string FailureMessage
        {
            get => $"{nameof(Poll)}: {_failureMessage} (interval: {_interval:g}, timeout: {_timeout:g})";
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _failureMessage = value;
            }
        }

        /// <summary>
        ///   <para>
        ///     Adds a filter to the polling operation to only run the polling operation
        ///     when the <typeparamref name="TException"/> represents a specific certain failure.
        ///   </para>
        ///   <para>Usually being used to describe 'transient' exceptions.</para>
        ///   <example>
        ///     <code>
        ///       options.AddExceptionFilter((RequestFailedException ex) => ex.StatusCode == 429);
        ///     </code>
        ///   </example>
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to throw during the polling operation.</typeparam>
        /// <param name="exceptionFilter">The custom filter to only select a subset of the given exceptions that needs to be polled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exceptionFilter"/> is <c>null</c>.</exception>
        /// Same as doing <seealso cref="Poll{TResut,TException}.When"/>
        public void AddExceptionFilter<TException>(Func<TException, bool> exceptionFilter)
        {
            ArgumentNullException.ThrowIfNull(exceptionFilter);
            _exceptionFilters.Add(ex => ex is TException typed && exceptionFilter(typed));
        }

        internal bool IsMatch(Exception exception)
        {
            return _exceptionFilters.Count is 0 || _exceptionFilters.Any(filter => filter(exception));
        }
    }

    /// <summary>
    /// <para>Represents a polling operation that will run until the target is available.</para>
    /// <para>See: <a href="https://testing.arcus-azure.net/features/core" /> for more information.</para>
    /// </summary>
    public static class Poll
    {
        /// <summary>
        /// Polls the given <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task UntilAvailableAsync(Func<Task> getTargetWithoutResultAsync)
        {
            return UntilAvailableAsync<Exception>(getTargetWithoutResultAsync);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TException">The type of exception that is expected to be thrown by the <paramref name="getTargetWithoutResultAsync"/>.</typeparam>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task UntilAvailableAsync<TException>(Func<Task> getTargetWithoutResultAsync)
            where TException : Exception
        {
            return UntilAvailableAsync<TException>(getTargetWithoutResultAsync, configureOptions: null);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the possible unavailable target.</param>
        /// <param name="configureOptions">The additional options to manipulate the polling behavior.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task UntilAvailableAsync(Func<Task> getTargetWithoutResultAsync, Action<PollOptions> configureOptions)
        {
            return UntilAvailableAsync<Exception>(getTargetWithoutResultAsync, configureOptions);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TException">The type of exception that is expected to be thrown by the <paramref name="getTargetWithoutResultAsync"/>.</typeparam>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the possible unavailable target.</param>
        /// <param name="configureOptions">The additional options to manipulate the polling behavior.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task UntilAvailableAsync<TException>(Func<Task> getTargetWithoutResultAsync, Action<PollOptions> configureOptions)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultAsync);

            var options = new PollOptions();
            configureOptions?.Invoke(options);

            return new Poll<TException>(getTargetWithoutResultAsync, options).StartAsync();
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <returns>The result of the interaction with the target.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task<TResult> UntilAvailableAsync<TResult>(Func<Task<TResult>> getTargetWithResultAsync)
        {
            return UntilAvailableAsync(getTargetWithResultAsync, configureOptions: null);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <param name="configureOptions">The additional options to manipulate the polling behavior.</param>
        /// <returns>The result of the interaction with the target.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task<TResult> UntilAvailableAsync<TResult>(Func<Task<TResult>> getTargetWithResultAsync, Action<PollOptions> configureOptions)
        {
            return UntilAvailableAsync<TResult, Exception>(getTargetWithResultAsync, configureOptions);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <typeparam name="TException">The type of exceptions the target can throw.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <returns>The result of the interaction with the target.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task<TResult> UntilAvailableAsync<TResult, TException>(Func<Task<TResult>> getTargetWithResultAsync)
            where TException : Exception
        {
            return UntilAvailableAsync<TResult, TException>(getTargetWithResultAsync, configureOptions: null);
        }

        /// <summary>
        /// Polls the given <paramref name="getTargetWithResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <typeparam name="TException">The type of exceptions the target can throw.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <param name="configureOptions">The additional options to manipulate the polling behavior.</param>
        /// <returns>The result of the interaction with the target.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Task<TResult> UntilAvailableAsync<TResult, TException>(Func<Task<TResult>> getTargetWithResultAsync, Action<PollOptions> configureOptions)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithResultAsync);

            var options = new PollOptions();
            configureOptions?.Invoke(options);

            return new Poll<TResult, TException>(getTargetWithResultAsync, options).StartAsync();
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithoutResultSync"/> until it stops throwing exceptions.
        /// </summary>
        /// <param name="getTargetWithoutResultSync">The custom function that interacts with the necessary unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultSync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<Exception> Target(Action getTargetWithoutResultSync)
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultSync);

            return Target<Exception>(() =>
            {
                getTargetWithoutResultSync();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<Exception> Target(Func<Task> getTargetWithoutResultAsync)
        {
            return Target<Exception>(getTargetWithoutResultAsync);
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithoutResultSync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <param name="getTargetWithoutResultSync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultSync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<TResult, Exception> Target<TResult>(Func<TResult> getTargetWithoutResultSync)
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultSync);
            return Target(() => Task.FromResult(getTargetWithoutResultSync()));
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<TResult, Exception> Target<TResult>(Func<Task<TResult>> getTargetWithResultAsync)
        {
            return Target<TResult, Exception>(getTargetWithResultAsync);
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithoutResultSync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TException">The type of exception the target can throw.</typeparam>
        /// <param name="getTargetWithoutResultSync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultSync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<TException> Target<TException>(Action getTargetWithoutResultSync) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultSync);

            return Target<TException>(() =>
            {
                getTargetWithoutResultSync();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithoutResultAsync"/> until it stops throwing exceptions.
        /// </summary>
        /// <typeparam name="TException">The type of exceptions the target can throw.</typeparam>
        /// <param name="getTargetWithoutResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithoutResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">Thrown when the target was not available (meaning: did not throw any exceptions) in the configured time frame.</exception>
        public static Poll<TException> Target<TException>(Func<Task> getTargetWithoutResultAsync)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithoutResultAsync);
            return new Poll<TException>(getTargetWithoutResultAsync, PollOptions.Default);
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithResultSync"/> until a given condition is met.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <typeparam name="TException">The type of exceptions the target can throw.</typeparam>
        /// <param name="getTargetWithResultSync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultSync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when the target was not available (meaning: did not throw any exceptions and the configured condition is met) in the configured time frame.
        /// </exception>
        public static Poll<TResult, TException> Target<TResult, TException>(Func<TResult> getTargetWithResultSync)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithResultSync);
            return Target<TResult, TException>(() => Task.FromResult(getTargetWithResultSync()));
        }

        /// <summary>
        /// Creates a polling operation that will run the <paramref name="getTargetWithResultAsync"/> until a given condition is met.
        /// </summary>
        /// <typeparam name="TResult">The type of result for which the target has to be polled.</typeparam>
        /// <typeparam name="TException">The type of exceptions the target can throw.</typeparam>
        /// <param name="getTargetWithResultAsync">The custom function that interacts with the necessary possible unavailable target.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="getTargetWithResultAsync"/> is <c>null</c>.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when the target was not available (meaning: did not throw any exceptions and the configured condition is met) in the configured time frame.
        /// </exception>
        public static Poll<TResult, TException> Target<TResult, TException>(Func<Task<TResult>> getTargetWithResultAsync)
            where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(getTargetWithResultAsync);
            return new Poll<TResult, TException>(getTargetWithResultAsync, PollOptions.Default);
        }
    }
}
