using System;
using System.IO;
using Arcus.Testing.Tests.Unit.Logging.Fixture;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    extern alias ArcusXunitV2;
    extern alias ArcusXunitV3;

    // ReSharper disable once InconsistentNaming
    [Trait(name: "Category", value: "Unit")]
    public class ILoggerBuilderExtensionsTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void AddXunitTestLogging_WithInfoMessage_LogsInfoMessage()
        {
            // Arrange
            var builder = new HostBuilder();
            var testOutput = new InMemoryTestOutputWriter();

            // Act
            builder.ConfigureLogging(logging => ArcusXunitV2::Microsoft.Extensions.Logging.ILoggerBuilderExtensions.AddXunitTestLogging(logging, testOutput));

            // Assert
            IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string exptected = Bogus.Lorem.Sentence();
            logger.LogInformation(exptected);
            Assert.Contains(testOutput.Contents, msg => msg.Contains(exptected));
        }

        [Fact]
        public void AddXunitV3TestLogging_WithInfoMessage_LogsInfoMessage()
        {
            // Arrange
            var builder = new HostBuilder();
            var testOutput = new InMemoryTestOutputWriter();

            // Act
            builder.ConfigureLogging(logging => ArcusXunitV3::Microsoft.Extensions.Logging.ILoggerBuilderExtensions.AddXunitTestLogging(logging, testOutput));

            // Assert
            IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string exptected = Bogus.Lorem.Sentence();
            logger.LogInformation(exptected);
            Assert.Contains(testOutput.Contents, msg => msg.Contains(exptected));
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
        public void AddNUnitTestLogging_WithError_LogsMessage()
        {
            // Arrange
            var mockWriter = new MockTestWriter();
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddNUnitTestLogging(TextWriter.Null, mockWriter));

            // Assert
            using IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string expected = Bogus.Lorem.Sentence();
            logger.LogError(expected);
            mockWriter.VerifyWritten(expected);
        }

        [Fact]
        public void AddNUnitTestLogging_WithoutOutputWriter_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            builder.ConfigureLogging(logging => logging.AddNUnitTestLogging(outputWriter: null));

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void AddNUnitTestLoggingWithError_WithoutOutputWriter_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            builder.ConfigureLogging(logging => logging.AddNUnitTestLogging(outputWriter: null, TextWriter.Null));

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void AddNUnitTestLoggingWithError_WithoutErrorWriter_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            builder.ConfigureLogging(logging => logging.AddNUnitTestLogging(TextWriter.Null, errorWriter: null));

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
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
        public void AddMSTestLogging_WithoutContext_Fails()
        {
            // Arrange
            var builder = new HostBuilder();
            builder.ConfigureLogging(logging => logging.AddMSTestLogging(testContext: null));

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void AddXunitTestLogging_WithoutXunitTestLogger_Throws()
        {
            // Arrange
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => ArcusXunitV2::Microsoft.Extensions.Logging.ILoggerBuilderExtensions.AddXunitTestLogging(logging, outputWriter: null));

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }

        [Fact]
        public void AddTUnitTestLogging_WithTestLogger_LogsMessage()
        {
            // Arrange
            var mockLogger = new MockTUnitTestLogger();
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddTUnitTestLogging(mockLogger));

            // Assert
            using IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string state = Bogus.Lorem.Word();
            using var _ = logger.BeginScope(state);

            string expected = Bogus.Lorem.Sentence();
            logger.LogInformation(expected);
            mockLogger.VerifyWritten(LogLevel.Information, expected, state: state);
        }

        [Fact]
        public void AddTUnitTestLogging_WithoutTestLogger_Throws()
        {
            // Arrange
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddTUnitTestLogging(outputWriter: null));

            // Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.Build());
        }
    }
}
