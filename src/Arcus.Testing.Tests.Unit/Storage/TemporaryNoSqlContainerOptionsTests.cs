using System;
using Bogus;
using Xunit;

#pragma warning disable CS0618 // Ignore obsolete warnings that we added ourselves, should be removed upon releasing v2.0.

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
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems((NoSqlItemFilter) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingItems(NoSqlItemFilter.IdEqual("some-id"), null));
        }

        [Fact]
        public void OnTeardown_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryNoSqlContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems((Func<Person, bool>) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems((Func<NoSqlItem, bool>) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems((NoSqlItemFilter) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingItems(NoSqlItemFilter.IdEqual("some-id"), null));
        }
    }
}
