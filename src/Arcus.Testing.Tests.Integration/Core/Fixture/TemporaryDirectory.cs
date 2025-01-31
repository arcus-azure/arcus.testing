using System;
using System.IO;
using System.Linq;
using Bogus;

namespace Arcus.Testing.Tests.Integration.Core.Fixture
{
    /// <summary>
    /// Represents a file directory that is temporary available on disk.
    /// </summary>
    internal class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _dir;
        private static readonly Faker Bogus = new();

        private TemporaryDirectory(DirectoryInfo dir, string[] subDirNames)
        {
            _dir = dir;
            SubDirNames = subDirNames;
        }

        /// <summary>
        /// Gets the sequence of sub-directory names that represents the path towards the inner-sub temporary directory (['foo', 'bar'] = '/foo/bar').
        /// </summary>
        public string[] SubDirNames { get; }

        /// <summary>
        /// Gets the directory path of this temporary directory.
        /// </summary>
        public DirectoryInfo Path => _dir;

        /// <summary>
        /// Generates a <see cref="TemporaryDirectory"/> at a given <paramref name="root"/> directory path.
        /// </summary>
        public static TemporaryDirectory GenerateAt(DirectoryInfo root)
        {
            ArgumentNullException.ThrowIfNull(root);

            string[] subDirNames = Bogus.Lorem.Words();
            string path = System.IO.Path.Combine(subDirNames.Prepend(root.FullName).ToArray());
            DirectoryInfo sub = Directory.CreateDirectory(path);

            return new TemporaryDirectory(sub, subDirNames);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _dir.Delete(recursive: true);
        }
    }
}