using System;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class ResourceNotFoundException
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
    }

    public class RemoteServiceUnavailableException : Exception
    {
        public RemoteServiceUnavailableException()
        {

        }

        public RemoteServiceUnavailableException(string message)
            : base(message)
        {

        }
    }

    public class RateLimitExceededException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException" /> class.
        /// </summary>
        public RateLimitExceededException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public RateLimitExceededException(string message)
            : base(message)
        {

        }
    }
}
