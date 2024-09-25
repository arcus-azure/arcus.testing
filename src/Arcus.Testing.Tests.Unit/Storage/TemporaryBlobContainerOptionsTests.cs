using System;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryBlobContainerOptionsTests
    {
        [Fact]
        public void CleanMatchingBlobsOnSetup_WithNullFilters_Fails()
        {
            // Arrange
            var options = new TemporaryBlobContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingBlobs(filters: null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingBlobs(BlobNameFilter.NameEqual("some-name"), null));
        }

        [Fact]
        public void CleanMatchingBlobsOnTeardown_WithNullFilters_Fails()
        {
            // Arrange
            var options = new TemporaryBlobContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(filters: null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(BlobNameFilter.NameEqual("some-name"), null));
        }
    }
}
