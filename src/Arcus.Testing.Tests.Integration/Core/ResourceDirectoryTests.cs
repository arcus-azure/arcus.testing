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
            AssertContainsFile(tempFile, subDir);
        }

        [Fact]
        public void FileContentsFromSubDirectory_FromUnknownFile_FailsWithNotFound()
        {
            // Arrange
            using var tempSubDir = TemporaryDirectory.GenerateAt(Root.Path);
            string unknownFileName = Bogus.Lorem.Word();

            // Act / Assert
            AssertFileNotFound(unknownFileName, Root);
        }

        [Fact]
        public void FileContentsFromRoot_FromKnownFile_SucceedsWithContents()
        {
            // Arrange
            using var tempFile = TemporaryFile.GenerateAt(Root.Path);

            // Act / Assert
            AssertContainsFile(tempFile, Root);
        }

        [Fact]
        public void FileContentsFromRoot_FromUnknownFile_FailsWithNotFound()
        {
            // Arrange
            string unknownFileName = Bogus.Lorem.Word();

            // Act / Assert
            AssertFileNotFound(unknownFileName, Root);
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

        private static void AssertContainsFile(TemporaryFile expectedFile, ResourceDirectory directory)
        {
            Assert.Equal(expectedFile.Contents, directory.ReadFileBytesByName(expectedFile.Name));
            Assert.Equal(expectedFile.Contents, directory.ReadFileBytesByPattern(expectedFile.Name[..^1] + "?"));
            Assert.Equal(expectedFile.Text, directory.ReadFileTextByName(expectedFile.Name));
            Assert.Equal(expectedFile.Text, directory.ReadFileTextByPattern(expectedFile.Name[..5] + "*"));
        }

        private static void AssertFileNotFound(string input, ResourceDirectory directory)
        {
            AssertFileNotFound(() => directory.ReadFileTextByName(input), input);
            AssertFileNotFound(() => directory.ReadFileBytesByName(input), input);
            AssertFileNotFound(() => directory.ReadFileTextByPattern(input), input);
            AssertFileNotFound(() => directory.ReadFileBytesByPattern(input), input);
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