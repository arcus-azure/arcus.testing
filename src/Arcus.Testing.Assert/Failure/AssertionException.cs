using System;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    [Serializable]
    public class AssertionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException" /> class.
        /// </summary>
        public AssertionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException" /> class.
        /// </summary>
        public AssertionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException" /> class.
        /// </summary>
        public AssertionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}