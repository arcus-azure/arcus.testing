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
            var logger = new MSTestLogger(mockContext);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            mockContext.VerifyWritten(message);
        }
    }
}
