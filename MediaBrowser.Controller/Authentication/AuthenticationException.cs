using System;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// The exception that is thrown when an attempt to authenticate fails.
    /// </summary>
    public class AuthenticationException : Exception
    {
        /// <inheritdoc />
        public AuthenticationException() : base()
        {

        }

        /// <inheritdoc />
        public AuthenticationException(string message) : base(message)
        {

        }

        /// <inheritdoc />
        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
