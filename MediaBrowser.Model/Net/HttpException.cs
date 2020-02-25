using System;
using System.Net;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class HttpException.
    /// </summary>
    public class HttpException : Exception
    {
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>The status code.</value>
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is timed out.
        /// </summary>
        /// <value><c>true</c> if this instance is timed out; otherwise, <c>false</c>.</value>
        public bool IsTimedOut { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public HttpException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public HttpException(string message)
            : base(message)
        {
        }
    }
}
