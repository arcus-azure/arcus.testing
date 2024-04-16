using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Arcus.Testing.Logging;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class NUnitTestLoggerTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void LogNoError_WithErrorStream_WritesToOutStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var mockErrorWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<NUnitTestLoggerTests>(mockOutWriter, mockErrorWriter);

            string message = Bogus.Lorem.Sentence();
            LogLevel level = Bogus.PickRandomWithout(LogLevel.Error);

            // Act
            logger.Log(level, message);

            // Assert
            mockOutWriter.VerifyWritten(message);
            mockErrorWriter.VerifyNotWritten(message);
        }

        [Fact]
        public void LogError_WithErrorStream_WritesToErrorStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var mockErrorWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<int>(mockOutWriter, mockErrorWriter);

            Exception exception = Bogus.System.Exception();
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogError(exception, message);

            // Assert
            mockErrorWriter.VerifyWritten(message, exception.ToString());
            mockOutWriter.VerifyNotWritten(message, exception.ToString());
        }

        [Fact]
        public void LogError_WithoutErrorStream_WritesToOutStream()
        {
            // Arrange
            var mockOutWriter = new MockTestWriter();
            var logger = new NUnitTestLogger<string>(mockOutWriter);

            Exception exception = Bogus.System.Exception();
            string message = Bogus.Lorem.Sentence();

            // Act
            logger.LogError(exception, message);

            // Assert
            mockOutWriter.VerifyWritten(message, exception.ToString());
        }


        private class MockTestWriter : TextWriter
        {
            private readonly ICollection<string> _messages = new Collection<string>();

            public override Encoding Encoding { get; } = Encoding.UTF8;

            public override void WriteLine(string value)
            {
                _messages.Add(value);
            }

            public void VerifyWritten(params string[] messages)
            {
                Assert.All(messages, msg => Assert.Contains(_messages, m => m.Contains(msg)));
            }

            public void VerifyNotWritten(params string[] messages)
            {
                Assert.All(messages, msg => Assert.DoesNotContain(_messages, m => m.Contains(msg)));
            }
        }
    }
}
