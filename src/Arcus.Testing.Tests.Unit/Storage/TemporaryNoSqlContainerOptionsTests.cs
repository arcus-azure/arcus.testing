using System;
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
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems(new NoSqlItemFilter[] { null } ));
        }

        [Fact]
        public void OnTeardown_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryNoSqlContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems(new NoSqlItemFilter[] { null } ));
        }
    }
}
