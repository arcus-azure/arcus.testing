using System;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Core
{
    public class TestConfigTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void Configure_WithoutNewMainFile_Fails(string newMainFile)
        {
            Assert.ThrowsAny<ArgumentException>(() => TestConfig.Create(opt => opt.UseMainJsonFile(newMainFile)));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Configure_WithoutNewOptionalFile_Fails(string newOptionalFile)
        {
            Assert.ThrowsAny<ArgumentException>(() => TestConfig.Create(opt => opt.AddOptionalJsonFile(newOptionalFile)));
        }
    }
}
