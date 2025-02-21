using System;
using Bogus;
using Xunit;

#pragma warning disable CS0618 // Ignore obsolete warnings that we added ourselves, should be removed upon releasing v2.0.

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class NoSqlItemFilterTests
    {
        private static readonly Faker Bogus = new();

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutItemId_Fails(string itemId)
        {
            Assert.ThrowsAny<ArgumentException>(() => NoSqlItemFilter.IdEqual(itemId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithComparisonWithoutItemId_Fails(string itemId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => NoSqlItemFilter.IdEqual(itemId, Bogus.PickRandom<StringComparison>()));
        }

        [Fact]
        public void CreateFilter_WithoutItemTFilter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => NoSqlItemFilter.Where<Person>(null));
        }

        [Fact]
        public void CreateFilter_WithoutItemFilter_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => NoSqlItemFilter.Where(null));
        }
    }
}
