﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Serilog;
using Serilog.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class LoggerSinkConfigurationExtensionsTests : ITestOutputHelper
    {
        private readonly ICollection<string> _messages = new Collection<string>();
        private static readonly Faker Bogus = new();

        [Fact]
        public void AddXunitTestLogging_WithXunitOutputWriter_Succeeds()
        {
            // Arrange
            var config = new LoggerConfiguration();
            
            // Act
            config.WriteTo.XunitTestLogging(this);

            // Assert
            ILogger logger = config.CreateLogger();
            string expected = Bogus.Lorem.Sentence();
            logger.Information(expected);
            Assert.Single(_messages, expected);
        }

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
        public void AddXunitTestLogging_WithoutOutputWriter_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.WriteTo.XunitTestLogging(outputWriter: null));
        }

        public void WriteLine(string message)
        {
            _messages.Add(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _messages.Add(string.Format(format, args));
        }
    }
}
