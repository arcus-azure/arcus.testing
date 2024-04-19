using System;
using System.IO;
using System.Linq;
using Arcus.Testing.Tests.Integration.Core.Fixture;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Integration.Core
{
    public class ResourceDirectoryTests
    {
        private static readonly Faker Bogus = new();

        private static ResourceDirectory Root => ResourceDirectory.CurrentDirectory;

        [Fact]
        public void FileContentsFromSubDirectory_FromKnownFile_SucceedsWithContents()
        {
            // Arrange
            using var tempSubDir = TemporaryDirectory.GenerateAt(Root.Path);
            using var tempFile = TemporaryFile.GenerateAt(tempSubDir.Path);

            ResourceDirectory subDir = WithSubDirectories(Root, tempSubDir);

            // Act / Assert
            Assert.Equal(tempFile.Contents, subDir.ReadFileBytesByName(tempFile.Name));
            Assert.Equal(tempFile.Text, subDir.ReadFileTextByName(tempFile.Name));
        }

        [Fact]
        public void FileContentsFromSubDirectory_FromUnknownFile_FailsWithNotFound()
        {
            // Arrange
            using var tempSubDir = TemporaryDirectory.GenerateAt(Root.Path);
            string unknownFileName = Bogus.Lorem.Word();

            // Act / Assert
            AssertFileNotFound(() => Root.ReadFileTextByName(unknownFileName), unknownFileName);
            AssertFileNotFound(() => Root.ReadFileBytesByName(unknownFileName), unknownFileName);
        }

        [Fact]
        public void FileContentsFromRoot_FromKnownFile_SucceedsWithContents()
        {
            // Arrange
            using var tempFile = TemporaryFile.GenerateAt(Root.Path);

            // Act / Assert
            Assert.Equal(tempFile.Contents, Root.ReadFileBytesByName(tempFile.Name));
            Assert.Equal(tempFile.Text, Root.ReadFileTextByName(tempFile.Name));
        }

        [Fact]
        public void FileContentsFromRoot_FromUnknownFile_FailsWithNotFound()
        {
            // Arrange
            string unknownFileName = Bogus.Lorem.Word();

            // Act / Assert
            AssertFileNotFound(() => Root.ReadFileTextByName(unknownFileName), unknownFileName);
            AssertFileNotFound(() => Root.ReadFileBytesByName(unknownFileName), unknownFileName);
        }

        [Fact]
        public void SubDirectory_FromKnownSubDirectory_SucceedsWithUpdatedPath()
        {
            // Arrange
            using var tempDir = TemporaryDirectory.GenerateAt(Root.Path);

            // Act
            ResourceDirectory subDir = WithSubDirectories(Root, tempDir);

            // Assert
            Assert.Equal(tempDir.Path.FullName, subDir.Path.FullName);
        }

        private static ResourceDirectory WithSubDirectories(ResourceDirectory resourceDir, TemporaryDirectory tempDir)
        {
            return tempDir.SubDirNames.Aggregate(resourceDir, (dir, name) => dir.WithSubDirectory(name));
        }

        [Fact]
        public void SubDirectory_FromUnknownSubDirectory_FailsWithNotFound()
        {
            // Arrange
            string unknownSubDirName = Bogus.Random.Guid().ToString();

            // Act / Assert
            AssertDirNotFound(() => Root.WithSubDirectory(unknownSubDirName), unknownSubDirName);
        }

        private static void AssertFileNotFound(Action dirAction, params string[] errorParts)
        {
            var exception = Assert.Throws<FileNotFoundException>(dirAction);
            Assert.Contains("resource directory", exception.Message);
            Assert.Contains(Root.Path.FullName, exception.Message);
            Assert.All(errorParts, part => Assert.Contains(part, exception.Message));
        }

        private static void AssertDirNotFound(Action dirAction, params string[] errorParts)
        {
            var exception = Assert.Throws<DirectoryNotFoundException>(dirAction);
            Assert.Contains("resource directory", exception.Message);
            Assert.Contains(Root.Path.FullName, exception.Message);
            Assert.All(errorParts, part => Assert.Contains(part, exception.Message));
        }
    }
}