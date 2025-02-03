using System;
using Bogus;
using MongoDB.Driver;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class TemporaryMongoDbCollectionOptionsTests
    {
        [Fact]
        public void OnSetup_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryMongoDbCollectionOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingDocuments<Person>(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingDocuments((FilterDefinition<Person>) null));
        }

        [Fact]
        public void OnTeardown_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryMongoDbCollectionOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingDocuments<Person>(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingDocuments((FilterDefinition<Person>) null));
        }
    }
}