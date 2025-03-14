using System;
using Azure.Storage.Blobs.Models;
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
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingBlobs(filters: (Func<BlobItem, bool>[]) null));
        }

        [Fact]
        public void CleanMatchingBlobsOnTeardown_WithNullFilters_Fails()
        {
            // Arrange
            var options = new TemporaryBlobContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(filters: (Func<BlobItem, bool>) null));
        }
    }
}
