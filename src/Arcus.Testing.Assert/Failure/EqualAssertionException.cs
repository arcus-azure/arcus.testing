using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    /// <summary>
    /// <para>Represents the exception implementation that gets thrown when an actual result does not match an expectation.</para>
    /// <para>See also: <see cref="AssertXml"/>, <see cref="AssertJson"/>.</para>
    /// </summary>
    [Serializable]
    public class EqualAssertionException : AssertionException
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualAssertionException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="info" /> is <see langword="null" />.</exception>
        /// <exception cref="SerializationException">Thrown when the class name is <see langword="null" /> or <see cref="Exception.HResult" /> is zero (0).</exception>
        protected EqualAssertionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}