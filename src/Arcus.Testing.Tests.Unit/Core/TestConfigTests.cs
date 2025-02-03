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
            // Arrange
            var options = new TestConfigOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.UseMainJsonFile(newMainFile));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Configure_WithoutNewOptionalFile_Fails(string newOptionalFile)
        {
            // Arrange
            var options = new TestConfigOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddOptionalJsonFile(newOptionalFile));
        }
    }
}