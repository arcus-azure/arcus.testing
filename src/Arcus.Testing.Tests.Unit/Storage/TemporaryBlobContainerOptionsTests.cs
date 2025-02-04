using System;
using Azure.Storage.Blobs.Models;
using Xunit;

#pragma warning disable CS0618 // Ignore obsolete warnings that we added ourselves, should be removed upon releasing v2.0.

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
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingBlobs(filters: (BlobNameFilter) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingBlobs(BlobNameFilter.NameEqual("some-name"), null));
        }

        [Fact]
        public void CleanMatchingBlobsOnTeardown_WithNullFilters_Fails()
        {
            // Arrange
            var options = new TemporaryBlobContainerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(filters: (Func<BlobItem, bool>) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(filters: (BlobNameFilter) null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingBlobs(BlobNameFilter.NameEqual("some-name"), null));
        }
    }
}
