#nullable enable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Common.Extensions
{
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
