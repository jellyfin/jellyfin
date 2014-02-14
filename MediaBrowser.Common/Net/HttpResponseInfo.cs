using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpResponseInfo
    /// </summary>
    public class HttpResponseInfo
    {
        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; }

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
        /// Gets or sets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public NameValueCollection Headers { get; set; }
    }
}
