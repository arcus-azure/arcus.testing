using System;
using System.Collections.ObjectModel;
using System.IO;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    [Trait(name: "Category", value: "Unit")]
    public class ILoggerBuilderExtensionsTests : ITestOutputHelper
    {
        private readonly Collection<string> _xUnitTestOutput = new();
        private static readonly Faker Bogus = new();

        [Fact]
        public void AddXunitTestLogging_WithInfoMessage_LogsInfoMessage()
        {
            // Arrange
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddXunitTestLogging(this));

            // Assert
            IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string exptected = Bogus.Lorem.Sentence();
            logger.LogInformation(exptected);
            Assert.Contains(_xUnitTestOutput, msg => msg.Contains(exptected));
        }

        [Fact]
        public void AddNUnitTestLogging_WithMessage_LogsMessage()
        {
            // Arrange
            var mockWriter = new MockTestWriter();
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddNUnitTestLogging(mockWriter));

            // Assert
            using IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string expected = Bogus.Lorem.Sentence();
            logger.LogInformation(expected);
            mockWriter.VerifyWritten(expected);
        }

        [Fact]
        public void AddMSTestLogging_WithMessage_LogsMessage()
        {
            // Arrange
            var mockContext = new MockTestContext();
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddMSTestLogging(mockContext));

            // Assert
            using IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string expected = Bogus.Lorem.Sentence();
            logger.LogInformation(expected);
            mockContext.VerifyWritten(expected);
        }

        [Fact]
        public void AddXunitTestLogging_WithoutBuilder_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((ILoggingBuilder) null).AddXunitTestLogging(Mock.Of<ITestOutputHelper>()));
        }

        [Fact]
        public void AddXunitTestLogging_WithoutXunitTestLogger_Throws()
        {
            // Arrange
            var builder = new HostBuilder();
            
            // Act
            builder.ConfigureLogging(logging => logging.AddXunitTestLogging(outputWriter: null));

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        public void WriteLine(string message)
        {
            _xUnitTestOutput.Add(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _xUnitTestOutput.Add(string.Format(format, args));
        }
    }
}
