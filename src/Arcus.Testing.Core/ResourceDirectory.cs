using System;
using System.IO;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a test-friendly resource directory where one or more resource files are located on disk.
    /// </summary>
    public class ResourceDirectory : IEquatable<ResourceDirectory>
    {
        private readonly DirectoryInfo _directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDirectory"/> class.
        /// </summary>
        /// <param name="directory">The directory that acts as the test resource directory on disk.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="directory"/> is <c>null</c>.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when no directory was found on disk for the given <paramref name="directory"/>.</exception>
        public ResourceDirectory(DirectoryInfo directory)
        {
            if (directory is null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!Directory.Exists(directory.FullName))
            {
                throw new DirectoryNotFoundException(
                    $"[Test] Cannot use test resource directory '{directory.Name}' because it does not exists on disk" +
                    Environment.NewLine +
                    $"Resource directory: {directory.FullName}");
            }

            _directory = directory;
        }

        /// <summary>
        /// Gets the path of the current resource directory.
        /// </summary>
        public DirectoryInfo Path => _directory;

        /// <summary>
        /// Gets the root application working directory as test resource directory.
        /// </summary>
        public static ResourceDirectory CurrentDirectory => new(new DirectoryInfo(Directory.GetCurrentDirectory()));

        /// <summary>
        /// Creates a test resource sub-directory based on the current test resource directory.
        /// </summary>
        /// <param name="subDirectoryName">The name of the sub-directory within the current test resource directory.</param>
        /// <returns>The test resource sub-directory.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subDirectoryName"/> is blank.</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     Thrown when no test resource sub-directory is found within the current test resource directory with the given <paramref name="subDirectoryName"/>.
        /// </exception>
        public ResourceDirectory WithSubDirectory(string subDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(subDirectoryName))
            {
                throw new ArgumentException(
                    $"Requires a non-blank sub-directory name to create a test resource sub-directory in the root test directory: '{_directory.FullName}'", nameof(subDirectoryName));
            }

            string newDirectoryPath = System.IO.Path.Combine(_directory.FullName, subDirectoryName);
            if (!Directory.Exists(newDirectoryPath))
            {
                throw new DirectoryNotFoundException(
                    $"[Test] Cannot use sub test resource directory '{subDirectoryName}' because it does not exists in resource directory '{Path.Name}'" +
                    Environment.NewLine +
                    $"Sub-directory path: {newDirectoryPath}" +
                    Environment.NewLine +
                    $"Resource directory: {Path.FullName}");
            }

            return new ResourceDirectory(new DirectoryInfo(newDirectoryPath));
        }

        /// <summary>
        /// Gets the file contents of a test resource file within the directory.
        /// </summary>
        /// <param name="fileName">The file name of the test resource.</param>
        /// <returns>The raw contents of the test resource file.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when there exists no test resource file in the current test resource directory with the given <paramref name="fileName"/>.
        /// </exception>
        public string ReadFileTextByName(string fileName)
        {
            FileInfo file = GetFileByPattern(fileName);
            return File.ReadAllText(file.FullName);
        }

        /// <summary>
        /// Gets the file contents of a test resource file within the directory that matches the specified <paramref name="searchPattern"/>.
        /// </summary>
        /// <param name="searchPattern">
        ///     The search string to match against the names of files in the directory.
        ///     This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.
        /// </param>
        /// <returns>The raw contents of the test resource file.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="searchPattern"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when there exists no test resource file in the current test resource directory with the given <paramref name="searchPattern"/>.
        /// </exception>
        public string ReadFileTextByPattern(string searchPattern)
        {
            FileInfo file = GetFileByPattern(searchPattern);
            return File.ReadAllText(file.FullName);
        }

        /// <summary>
        /// Gets the file contents of a test resource file within the directory.
        /// </summary>
        /// <param name="fileName">The file name of the test resource.</param>
        /// <returns>The raw contents of the test resource file.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when there exists no test resource file in the current test resource directory with the given <paramref name="fileName"/>.
        /// </exception>
        public byte[] ReadFileBytesByName(string fileName)
        {
            FileInfo file = GetFileByPattern(fileName);
            return File.ReadAllBytes(file.FullName);
        }

        /// <summary>
        /// Gets the file contents of a test resource file within the directory that matches the specified <paramref name="searchPattern"/>.
        /// </summary>
        /// <param name="searchPattern">
        ///     The search string to match against the names of files in the directory.
        ///     This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.
        /// </param>
        /// <returns>The raw contents of the test resource file.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="searchPattern"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when there exists no test resource file in the current test resource directory with the given <paramref name="searchPattern"/>.
        /// </exception>
        public byte[] ReadFileBytesByPattern(string searchPattern)
        {
            FileInfo file = GetFileByPattern(searchPattern);
            return File.ReadAllBytes(file.FullName);
        }

        private FileInfo GetFileByPattern(string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                throw new ArgumentException("Requires a non-blank search pattern to retrieve the contents of a test resource file in the resource directory", nameof(searchPattern));
            }

            FileInfo[] files = _directory.GetFiles(searchPattern);
            if (files.Length == 0)
            {
                throw new FileNotFoundException(
                    $"[Test] Cannot retrieve '{searchPattern}' file contents in the resource directory because it does not exists, " +
                    "make sure that the test resource files are always copied to the output before loading their contents. " +
                    Environment.NewLine +
                    $"Search pattern: {searchPattern}" +
                    Environment.NewLine +
                    $"Resource directory: {CurrentDirectory.Path.FullName}");
            }

            if (files.Length > 1)
            {
                throw new FileNotFoundException(
                    $"[Test] Cannot retrieve '{searchPattern}' file contents in the resource directory because there are multiple files found, " +
                    "make sure that the test resource files are always copied to the output before loading their contents. " +
                    Environment.NewLine +
                    $"Search pattern: {searchPattern}" +
                    Environment.NewLine +
                    $"Resource directory: {CurrentDirectory.Path.FullName}");
            }

            return files[0];
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
        public bool Equals(ResourceDirectory other)
        {
            if (other is null)
            {
                return false;
            }

            return _directory.FullName.Equals(other._directory.FullName, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            return obj is ResourceDirectory other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return _directory.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(ResourceDirectory left, ResourceDirectory right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether the specified object is not equal to the current object.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(ResourceDirectory left, ResourceDirectory right)
        {
            ArgumentNullException.ThrowIfNull(left);
            return !left.Equals(right);
        }

        /// <summary>
        /// Appends to the <paramref name="current"/> test resource directory a sub-directory based on the given <paramref name="subDirectoryName"/>.
        /// </summary>
        /// <param name="current">The subject test resource directory currently.</param>
        /// <param name="subDirectoryName">The name of the sub-directory within the current test resource directory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subDirectoryName"/> is blank.</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     Thrown when no test resource sub-directory is found within the current test resource directory with the given <paramref name="subDirectoryName"/>.
        /// </exception>
        public static ResourceDirectory operator /(ResourceDirectory current, string subDirectoryName)
        {
            ArgumentNullException.ThrowIfNull(current);
            return current.WithSubDirectory(subDirectoryName);
        }
    }
}
