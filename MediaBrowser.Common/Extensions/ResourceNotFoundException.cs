using System;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class ResourceNotFoundException.
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNotFoundException" /> class.
        /// </summary>
        public ResourceNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNotFoundException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ResourceNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
