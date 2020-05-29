#nullable enable

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
    }
}
