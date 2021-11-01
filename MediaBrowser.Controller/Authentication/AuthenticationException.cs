using System;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// The exception that is thrown when an attempt to authenticate fails.
    /// </summary>
    public class AuthenticationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
        /// </summary>
        public AuthenticationException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AuthenticationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
