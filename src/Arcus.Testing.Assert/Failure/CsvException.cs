using System;
using System.Runtime.Serialization;

namespace Arcus.Testing.Failure
{
    /// <summary>
    /// Represents an exception that is thrown when invalid CSV is encountered.
    /// </summary>
    [Serializable]
    public class CsvException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvException" /> class.
        /// </summary>
        public CsvException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public CsvException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CsvException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="info" /> is <see langword="null" />.</exception>
        /// <exception cref="SerializationException">Thrown when the class name is <see langword="null" /> or <see cref="Exception.HResult" /> is zero (0).</exception>
        protected CsvException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}