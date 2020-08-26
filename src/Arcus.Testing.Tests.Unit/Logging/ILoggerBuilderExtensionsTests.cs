using System;
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
    public class ILoggerBuilderExtensionsTests
    {
        [Fact]
        public void AddXunitTestLogging_WithInfoMessage_LogsInfoMessage()
        {
            // Arrange
            var spyLogger = new Mock<ITestOutputHelper>();
            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddXunitTestLogging(spyLogger.Object));

            // Assert
            IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<ILoggerBuilderExtensionsTests>>();

            string exptected = "This informational message should be logged";
            logger.LogInformation(exptected);

            spyLogger.Verify(l => l.WriteLine(It.Is<string>(msg => msg.EndsWith(exptected))));
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
    }
}
