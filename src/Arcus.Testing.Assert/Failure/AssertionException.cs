using System;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    /// <summary>
    /// Represents the root exception for any test assertions failure in the library.
    /// </summary>
#pragma warning disable S3925 // Custom exceptions are serializable by default in modern .NET.
    public class AssertionException : Exception
#pragma warning restore S3925
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
