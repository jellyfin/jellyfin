#pragma warning disable CS1591

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

    /// <summary>
    /// Class MethodNotAllowedException
    /// </summary>
    public class MethodNotAllowedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException" /> class.
        /// </summary>
        public MethodNotAllowedException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MethodNotAllowedException(string message)
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
