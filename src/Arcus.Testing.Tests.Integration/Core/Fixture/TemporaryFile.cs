using System;
using System.IO;
using System.Text;
using Bogus;
using GuardNet;

namespace Arcus.Testing.Tests.Integration.Core.Fixture
{
    /// <summary>
    /// Represents a file that is temporary available on disk.
    /// </summary>
    internal class TemporaryFile : IDisposable
    {
        private readonly FileInfo _file;
        private static readonly Faker Bogus = new();

        private TemporaryFile(FileInfo file, byte[] fileContents)
        {
            Guard.NotNull(file, nameof(file));
            Guard.NotNull(fileContents, nameof(fileContents));

            _file = file;
            Contents = fileContents;
        }

        /// <summary>
        /// Gets the name of the temporary file.
        /// </summary>
        public string Name => _file.Name;

        /// <summary>
        /// Gets the raw contents of the temporary file.
        /// </summary>
        public byte[] Contents { get; }
        
        /// <summary>
        /// Gets the raw contents as text of the temporary file.
        /// </summary>
        public string Text => Encoding.UTF8.GetString(Contents);

        /// <summary>
        /// Generates a <see cref="TemporaryFile"/> at the given <paramref name="directory"/> path.
        /// </summary>
        public static TemporaryFile GenerateAt(DirectoryInfo directory)
        {
            Guard.NotNull(directory, nameof(directory));

            string fileName = Bogus.System.FileName();
            byte[] fileContents = Bogus.Random.Bytes(Bogus.Random.Int(10, 20));

            return CreateAt(directory, fileName, fileContents);
        }

        /// <summary>
        /// Creates a <see cref="TemporaryFile"/> at the given <paramref name="directory"/> path.
        /// </summary>
        public static TemporaryFile CreateAt(DirectoryInfo directory, string fileName, byte[] fileContents)
        {
            Guard.NotNull(directory, nameof(directory));
            Guard.NotNullOrWhitespace(fileName, nameof(fileContents));
            Guard.NotNull(fileContents, nameof(fileContents));

            string filePath = Path.Combine(directory.FullName, fileName);
            File.WriteAllBytes(filePath, fileContents);

            return new TemporaryFile(new FileInfo(filePath), fileContents);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _file.Delete();
        }
    }
}
