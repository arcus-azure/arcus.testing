using System;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    /// <summary>
    /// <para>Represents the exception implementation that gets thrown when an actual result does not match an expectation.</para>
    /// <para>See also: <see cref="AssertXml"/>, <see cref="AssertJson"/>.</para>
    /// </summary>
    [Serializable]
#pragma warning disable S3925 // Custom exceptions are serializable by default in modern .NET.
    public class EqualAssertionException : AssertionException
#pragma warning restore S3925
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EqualAssertionException" /> class.
        /// </summary>
        public EqualAssertionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualAssertionException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the failure.</param>
        public EqualAssertionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualAssertionException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the failure.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EqualAssertionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
