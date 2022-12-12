using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcus.Testing.Logging.Extensions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class LoggerSinkConfigurationExtensionsTests : ITestOutputHelper
    {
        private readonly ICollection<string> _messages = new Collection<string>();

        [Fact]
        public void AddXunitTestLogging_WithXunitOutputWriter_Succeeds()
        {
            // Arrange
            var config = new LoggerConfiguration();
            
            // Act
            config.WriteTo.XunitTestLogging(this);

            // Assert
            ILogger logger = config.CreateLogger();
            var expected = "This information message should be present in the xUnit test output writer";
            logger.Information(expected);
            Assert.Single(_messages, expected);
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
