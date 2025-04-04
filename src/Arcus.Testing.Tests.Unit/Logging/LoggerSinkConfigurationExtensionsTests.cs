using System;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Serilog;
using Serilog.Configuration;
using Xunit;

#pragma warning disable CS0618 // Serilog dependency is deprecated in implementation, but still tested here.

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class LoggerSinkConfigurationExtensionsTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void AddMSTestLogging_WithMessage_LogsMessage()
        {
            // Arrange
            var mockContext = new MockTestContext();
            var config = new LoggerConfiguration();

            // Act
            config.WriteTo.MSTestLogging(mockContext);

            // Assert
            ILogger logger = config.CreateLogger();
            string expected = Bogus.Lorem.Sentence();
            logger.Information(expected);
            mockContext.VerifyWritten(expected);
        }

        [Fact]
        public void AddMSTestTestLogging_WithoutTestContext_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.WriteTo.MSTestLogging(testContext: null));
        }

        [Fact]
        public void MsTestLogSink_WithoutTestContext_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new MSTestLogEventSink(context: null));
        }
    }
}
