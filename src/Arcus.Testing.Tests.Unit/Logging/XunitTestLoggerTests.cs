using System.Collections.Generic;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class XunitTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void XunitLog_WithMessage_Succeeds()
        {
            // Arrange
            var spyWriter = new SpyTestWriter();
            var logger = new XunitTestLogger(spyWriter);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Contains(spyWriter.Messages, msg => msg.Contains(message));
        }

        [Fact]
        public void XunitTLog_WithMessage_Succeeds()
        {
            // Arrange
            var spyWriter = new SpyTestWriter();
            var logger = new XunitTestLogger<XunitTestLoggerTests>(spyWriter);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Contains(spyWriter.Messages, msg => msg.Contains(message));
        }

        private class SpyTestWriter : ITestOutputHelper
        {
            public List<string> Messages { get; } = new List<string>();
            public void WriteLine(string message) => Messages.Add(message);
            public void WriteLine(string format, params object[] args)
            {
                Messages.Add(string.Format(format, args));
            }
        }
    }
}
