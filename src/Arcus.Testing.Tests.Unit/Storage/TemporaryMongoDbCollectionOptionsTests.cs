using System;
using Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture;
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
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingDocuments<Order>(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnSetup.CleanMatchingDocuments((FilterDefinition<Order>) null));
        }

        [Fact]
        public void OnTeardown_WithNullFilter_Fails()
        {
            // Arrange
            var options = new TemporaryMongoDbCollectionOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingDocuments<Order>(null));
            Assert.ThrowsAny<ArgumentException>(() => options.OnTeardown.CleanMatchingDocuments((FilterDefinition<Order>) null));
        }
    }
}
