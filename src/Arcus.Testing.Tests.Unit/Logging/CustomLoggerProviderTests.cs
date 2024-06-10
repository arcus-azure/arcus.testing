using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    [Trait(name: "Category", value: "Unit")]
    public class CustomLoggerProviderTests
    {
        [Fact]
        public void AddCustomLoggerProvider_WithInMemoryLogger_CollectsLogMessages()
        {
            // Arrange
            var spyLogger = new InMemoryLogger();
            var provider = new CustomLoggerProvider(spyLogger);

            var builder = new HostBuilder();

            // Act
            builder.ConfigureLogging(logging => logging.AddProvider(provider));

            // Assert
            IHost host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<CustomLoggerProviderTests>>();

            string expected = "This informational message should be logged";
            logger.LogInformation(expected);

            string actual = Assert.Single(spyLogger.Messages);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateProvider_WithoutLogger_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => new CustomLoggerProvider(logger: null));
        }
    }
}
