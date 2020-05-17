using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpResponseInfo.
    /// </summary>
    public sealed class HttpResponseInfo : IDisposable
    {
#pragma warning disable CS1591
        public HttpResponseInfo()
        {
        }

        public HttpResponseInfo(HttpResponseHeaders headers, HttpContentHeaders contentHeader)
        {
            Headers = headers;
            ContentHeaders = contentHeader;
        }

#pragma warning restore CS1591

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the response URL.
        /// </summary>
        /// <value>The response URL.</value>
        public string ResponseUrl { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        public Stream Content { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>The status code.</value>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the temp file path.
        /// </summary>
        /// <value>The temp file path.</value>
        public string TempFilePath { get; set; }

        /// <summary>
        /// Gets or sets the length of the content.
        /// </summary>
        /// <value>The length of the content.</value>
        public long? ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public HttpResponseHeaders Headers { get; set; }

        /// <summary>
        /// Gets or sets the content headers.
        /// </summary>
        /// <value>The content headers.</value>
        public HttpContentHeaders ContentHeaders { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            // backwards compatibility
        }
    }
}
