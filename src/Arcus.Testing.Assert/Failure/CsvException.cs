using System;

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
    }
}
