using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpResponseInfo
    /// </summary>
    public class HttpResponseInfo : IDisposable
    {
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
        public Dictionary<string,string> Headers { get; set; }

        private readonly IDisposable _disposable;

        public HttpResponseInfo(IDisposable disposable)
        {
            _disposable = disposable;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public HttpResponseInfo()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
            }
        }
    }
}
