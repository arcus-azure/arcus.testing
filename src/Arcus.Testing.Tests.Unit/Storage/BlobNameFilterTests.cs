using System;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Storage
{
    public class BlobNameFilterTests
    {
        private static readonly Faker Bogus = new();

        [Theory]
        [ClassData(typeof(Blanks))]
        public void NameEqual_WithoutName_Fails(string name)
        {
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameEqual(name));
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameEqual(name, Bogus.PickRandom<StringComparison>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void NameContains_WithoutValue_Fails(string value)
        {
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameContains(value));
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameContains(value, Bogus.PickRandom<StringComparison>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void NameStartsWith_WithoutPrefix_Fails(string prefix)
        {
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameStartsWith(prefix));
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameStartsWith(prefix, Bogus.PickRandom<StringComparison>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void NameEndsWith_WithoutSuffix_Fails(string suffix)
        {
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameEndsWith(suffix));
            Assert.ThrowsAny<ArgumentException>(() => BlobNameFilter.NameEndsWith(suffix, Bogus.PickRandom<StringComparison>()));
        }
    }
}
