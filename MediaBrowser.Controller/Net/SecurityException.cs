using System;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// The exception that is thrown when a user is authenticated, but not authorized to access a requested resource.
    /// </summary>
    public class SecurityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityException"/> class.
        /// </summary>
        public SecurityException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SecurityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public SecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
