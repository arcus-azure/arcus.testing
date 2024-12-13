using System;
using System.IO;

namespace Arcus.Testing.Tests.Integration.Core.Fixture
{
    /// <summary>
    /// Represents a temporary edit operation on an available file on disk.
    /// </summary>
    internal class TemporaryFileEdit : IDisposable
    {
        private readonly FileInfo _file;
        private readonly string _originalContents;

        private TemporaryFileEdit(FileInfo file, string originalContents)
        {
            ArgumentNullException.ThrowIfNull(file);

            _file = file;
            _originalContents = originalContents;
        }

        /// <summary>
        /// Edits a <paramref name="file"/> during the lifetime of the <see cref="TemporaryFileEdit"/>.
        /// </summary>
        public static TemporaryFileEdit At(FileInfo file, Func<string, string> editContents)
        {
            string originalContents = File.ReadAllText(file.FullName);
            string editedContents = editContents(originalContents);

            File.WriteAllText(file.FullName, editedContents);
            return new TemporaryFileEdit(file, originalContents);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            File.WriteAllText(_file.FullName, _originalContents);
        }
    }
}
