using System;
using System.Collections.Generic;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    extern alias ArcusXunitV2;
    extern alias ArcusXunitV3;
    public class XunitTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void XunitLog_WithMessage_Succeeds()
        {
            // Arrange
            var spyWriter = new SpyTestWriter();
            var logger = new ArcusXunitV2::Arcus.Testing.XunitTestLogger(spyWriter);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Contains(spyWriter.Messages, msg => msg.Contains(message));
        }

        [Fact]
        public void XunitV3Log_WithMessage_Succeeds()
        {
            // Arrange
            var spyWriter = new SpyTestWriter();
            var logger = new ArcusXunitV3::Arcus.Testing.XunitTestLogger(spyWriter);
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
            var logger = new ArcusXunitV2::Arcus.Testing.XunitTestLogger<XunitTestLoggerTests>(spyWriter);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Contains(spyWriter.Messages, msg => msg.Contains(message));
        }

        [Fact]
        public void XunitV3TLog_WithMessage_Succeeds()
        {
            // Arrange
            var spyWriter = new SpyTestWriter();
            var logger = new ArcusXunitV3::Arcus.Testing.XunitTestLogger<XunitTestLoggerTests>(spyWriter);
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Contains(spyWriter.Messages, msg => msg.Contains(message));
        }

        [Fact]
        public void Create_WithoutWriter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new ArcusXunitV2::Arcus.Testing.XunitTestLogger(testOutput: null));
        }

        [Fact]
        public void CreateV3_WithoutWriter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new ArcusXunitV3::Arcus.Testing.XunitTestLogger(outputWriter: null));
        }

        private class SpyTestWriter : Xunit.Abstractions.ITestOutputHelper, ITestOutputHelper
        {
            public List<string> Messages { get; } = new();
            public void Write(string message) => Messages.Add(message);
            public void Write(string format, params object[] args) => Write(string.Format(format, args));

            public void WriteLine(string message) => Messages.Add(message);
            public void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));

            public string Output => string.Join(Environment.NewLine, Messages);
        }
    }
}
