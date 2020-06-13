#nullable enable

using System;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class MethodNotAllowedException.
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
}
