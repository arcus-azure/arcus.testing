using System;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryNoSqlContainerOptionsTests
    {
        [Fact]
        public void OnSetup_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryNoSqlContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems((Func<Person, bool>) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems((Func<NoSqlItem, bool>) null));
        }

        [Fact]
        public void OnTeardown_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryNoSqlContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems((Func<Person, bool>) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems((Func<NoSqlItem, bool>) null));
        }
    }
}
