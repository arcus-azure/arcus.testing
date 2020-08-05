using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Testing.Logging;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit
{
    [Trait(name: "Category", value: "Unit")]
    public class SpyLoggerTests
    {
        public static IEnumerable<object[]> Loggers =>
            new[]
            {
                new object[] { new SpyLogger() },
                new object[] { new SpyLogger<object>() }
            };

        [Theory]
        [MemberData(nameof(Loggers))]
        public void LogInformation_WithArguments_GetsCollected(SpyLogger logger)
        {
            // Arrange
            const string template = "This is a test message with args: {Args}";
            const string args = "something to inject";

            // Act
            logger.LogInformation(template, args);

            // Assert
            string expected = template.Replace("{Args}", args);
            string actual = Assert.Single(logger.Messages);
            Assert.Equal(expected, actual);
            LogEntry entry = Assert.Single(logger.Entries);
            Assert.NotNull(entry);
            Assert.Equal(expected, entry.Message);
            Assert.Equal(LogLevel.Information, entry.Level);
        }

        [Theory]
        [MemberData(nameof(Loggers))]
        public void LogWarning_WithException_GetsCollected(SpyLogger logger)
        {
            // Arrange
            const string message = "This is a warning!";
            var exception = new Exception("Something happened!");

            // Act
            logger.LogWarning(exception, message);

            // Assert
            string actual = Assert.Single(logger.Messages);
            Assert.Equal(message, actual);
            LogEntry entry = Assert.Single(logger.Entries);
            Assert.NotNull(entry);
            Assert.Equal(message, entry.Message);
            Assert.Equal(LogLevel.Warning, entry.Level);
        }

        [Theory]
        [MemberData(nameof(Loggers))]
        public void LogSeveralMessages_WithRandomValues_GetsCollected(SpyLogger logger)
        {
            // Arrange
            var generator = new Faker();
            int messageCount = generator.Random.Int(5, 10);
            string[] messages = generator.Random.WordsArray(messageCount);
            LogEntry[] entries = messages.Select(msg =>
            {
                var level = generator.PickRandom<LogLevel>();
                return new LogEntry(new EventId(), level, msg, exception: null);
            }).ToArray();

            // Act / Assert
            Assert.All(entries, entry => logger.Log(entry.Level, entry.Message));
            Assert.True(messages.SequenceEqual(logger.Messages), "Should collected all messages");
            Assert.Equal(entries.Length, logger.Entries.Count());
            Assert.All(entries, entry =>
            {
                Assert.Contains(logger.Entries, e =>
                {
                    return e.Message == entry.Message && e.Level == entry.Level;
                });
            });
        }
    }
}
