using System;
using Arcus.Testing.Logging;
using Bogus;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    [Trait(name: "Category", value: "Unit")]
    public class InMemoryLogSinkTests
    {
        private static readonly Faker BogusGenerator = new Faker();
        
        [Fact]
        public void LogsEvent_WithTestSink_CollectsEmits()
        {
            // Arrange
            string expected = BogusGenerator.Lorem.Sentence();
            var spySink = new InMemoryLogSink();
            var configuration = new LoggerConfiguration().WriteTo.Sink(spySink);
            using (Logger logger = configuration.CreateLogger())
            {
                // Act
                logger.Information(expected);
            }

            // Assert
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.Equal(LogEventLevel.Information, logEvent.Level);
            Assert.Equal(expected, logEvent.RenderMessage());
            string actual = Assert.Single(spySink.CurrentLogMessages);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LogEvents_WithTestSink_CollectsFirstInFirstOut()
        {
            // Arrange
            Exception exception = BogusGenerator.System.Exception();
            string errorMessage = BogusGenerator.Lorem.Sentence();
            string traceMessage = BogusGenerator.Lorem.Sentence();
            
            var spySink = new InMemoryLogSink();
            var configuration = 
                new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Sink(spySink);
            
            using (Logger logger = configuration.CreateLogger())
            {
                // Act
                logger.Error(exception, errorMessage);
                logger.Verbose(traceMessage);
            }

            // Assert
            Assert.Collection(spySink.CurrentLogEmits,
                emit =>
                {
                    Assert.Equal(LogEventLevel.Error, emit.Level);
                    Assert.Equal(errorMessage, emit.RenderMessage());
                    Assert.Equal(exception, emit.Exception);
                },
                emit =>
                {
                    Assert.Equal(LogEventLevel.Verbose, emit.Level);
                    Assert.Equal(traceMessage, emit.RenderMessage());
                });
            
            Assert.Collection(spySink.CurrentLogMessages, 
                message => Assert.Equal(errorMessage, message), 
                message => Assert.Equal(traceMessage, message));
        }
    }
}
