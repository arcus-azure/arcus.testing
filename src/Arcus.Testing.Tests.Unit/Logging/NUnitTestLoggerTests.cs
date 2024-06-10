using System;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class NUnitTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void LogNoError_WithErrorStream_WritesToOutStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var mockErrorWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<NUnitTestLoggerTests>(mockOutWriter, mockErrorWriter);

            string message = Bogus.Lorem.Sentence();
            LogLevel level = Bogus.PickRandomWithout(LogLevel.Error);

            // Act
            logger.Log(level, message);

            // Assert
            mockOutWriter.VerifyWritten(message);
            mockErrorWriter.VerifyNotWritten(message);
        }

        [Fact]
        public void LogError_WithErrorStream_WritesToErrorStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var mockErrorWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<int>(mockOutWriter, mockErrorWriter);

            Exception exception = Bogus.System.Exception();
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogError(exception, message);

            // Assert
            mockErrorWriter.VerifyWritten(message, exception.ToString());
            mockOutWriter.VerifyNotWritten(message, exception.ToString());
        }

        [Fact]
        public void LogError_WithoutErrorStream_WritesToOutStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<string>(mockOutWriter);

            Exception exception = Bogus.System.Exception();
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogError(exception, message);

            // Assert
            mockOutWriter.VerifyWritten(message, exception.ToString());
        }
    }
}
