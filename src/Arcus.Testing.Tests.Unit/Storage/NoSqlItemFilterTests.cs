using System;
using Arcus.Testing.Tests.Unit.Messaging.ServiceBus.Fixture;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class NoSqlItemFilterTests
    {
        private static readonly Faker Bogus = new();

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutItemId_Fails(string itemId)
        {
            Assert.ThrowsAny<ArgumentException>(() => NoSqlItemFilter.ItemIdEqual(itemId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithComparisonWithoutItemId_Fails(string itemId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => NoSqlItemFilter.ItemIdEqual(itemId, Bogus.PickRandom<StringComparison>()));
        }

        [Fact]
        public void CreateFilter_WithoutItemFilter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => NoSqlItemFilter.ItemEqual<Shipment>(null));
        }
    }
}
