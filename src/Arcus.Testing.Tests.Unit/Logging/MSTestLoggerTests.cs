using System;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class MSTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void Log_WithMessage_Succeeds()
        {
            // Arrange
            var mockContext = new MockTestContext();
            var logger = new MSTestLogger<MSTestLoggerTests>(mockContext);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            mockContext.VerifyWritten(message);
        }

        [Fact]
        public void Log_WithError_Succeeds()
        {
            // Arrange
            var mockContext = new MockTestContext();
            var logger = new MSTestLogger<int>(mockContext);
            Exception exception = Bogus.System.Exception();
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogError(exception, message);

            // Assert
            mockContext.VerifyWritten(message);
        }

        [Fact]
        public void Create_WithoutContext_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new MSTestLogger(testContext: null));
        }
    }
}
