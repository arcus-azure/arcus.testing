using System;

// ReSharper disable once CheckNamespace - place the exceptions in the root namespace for less clutter when exception is written to test output.
namespace Arcus.Testing
{
    /// <summary>
    /// <para>Represents the exception implementation that gets thrown when an actual result does not matches an expectation.</para>
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
    }
}