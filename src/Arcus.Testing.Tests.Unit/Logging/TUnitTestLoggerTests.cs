using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class TUnitTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void Log_WithLevel_SucceedsWithMicrosoftLevel()
        {
            // Arrange
            string expectedMessage = Bogus.Lorem.Sentence();
            var expectedLevel = Bogus.PickRandom<LogLevel>();

            var mockLogger = new MockTUnitTestLogger();
            var logger = new TUnitTestLogger(mockLogger);

            // Act
            logger.Log(expectedLevel, expectedMessage);

            // Assert
            mockLogger.VerifyWritten(expectedLevel, expectedMessage);
        }
    }
}
