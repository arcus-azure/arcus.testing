using System;
using System.IO;
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
        public void AddNUnitTestLogging_WithMessage_LogsMessage()
        {
            // Arrange
            var mockWriter = new MockTestWriter();
            var config = new LoggerConfiguration();

            // Act
            config.WriteTo.NUnitTestLogging(mockWriter);

            // Assert
            ILogger logger = config.CreateLogger();
            string expected = Bogus.Lorem.Sentence();
            logger.Information(expected);
            mockWriter.VerifyWritten(expected);
        }

        [Fact]
        public void AddNUnitTestLogging_WithError_LogsMessage()
        {
            // Arrange
            var mockWriter = new MockTestWriter();
            var config = new LoggerConfiguration();

            // Act
            config.WriteTo.NUnitTestLogging(TextWriter.Null, mockWriter);

            // Assert
            ILogger logger = config.CreateLogger();
            string expected = Bogus.Lorem.Sentence();
            logger.Error(expected);
            mockWriter.VerifyWritten(expected);
        }

        [Fact]
        public void AddNUnitTestLogging_WithoutOutputWriter_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.WriteTo.NUnitTestLogging(outputWriter: null));
        }

        [Fact]
        public void AddNUnitTestLoggingWithError_WithoutOutputWriter_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.WriteTo.NUnitTestLogging(outputWriter: null, TextWriter.Null));
        }

        [Fact]
        public void AddNUnitTestLoggingWithError_WithoutErrorWriter_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.WriteTo.NUnitTestLogging(TextWriter.Null, errorWriter: null));
        }
    }
}
