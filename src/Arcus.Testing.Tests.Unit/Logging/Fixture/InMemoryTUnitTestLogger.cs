using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TUnit.Core.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging.Fixture
{
    public class MockTUnitTestLogger : ILogger
    {
        internal Collection<(LogLevel level, string message)> Logs { get; } = [];

        public ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifies that there was a <paramref name="message"/> written for the given <paramref name="level"/> to this logger.
        /// </summary>
        public void VerifyWritten(Microsoft.Extensions.Logging.LogLevel level, string message)
        {
            Assert.Contains(Logs, log => (int) level == (int) log.level && message == log.message);
        }
    }
}