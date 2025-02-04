using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    /// <summary>
    /// Represents the root exception for any test assertions failure in the library.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="info" /> is <see langword="null" />.</exception>
        /// <exception cref="SerializationException">Thrown when the class name is <see langword="null" /> or <see cref="Exception.HResult" /> is zero (0).</exception>
        protected AssertionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}