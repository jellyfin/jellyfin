#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpRequestOptions.
    /// </summary>
    public class HttpRequestOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestOptions"/> class.
        /// </summary>
        public HttpRequestOptions()
        {
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            CacheMode = CacheMode.None;
            DecompressionMethod = CompressionMethods.Deflate;
        }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        public CompressionMethods DecompressionMethod { get; set; }

        /// <summary>
        /// Gets or sets the accept header.
        /// </summary>
        /// <value>The accept header.</value>
        public string AcceptHeader
        {
            get => GetHeaderValue(HeaderNames.Accept);
            set => RequestHeaders[HeaderNames.Accept] = value;
        }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        /// <value>The cancellation token.</value>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>The user agent.</value>
        public string UserAgent
        {
            get => GetHeaderValue(HeaderNames.UserAgent);
            set => RequestHeaders[HeaderNames.UserAgent] = value;
        }

        /// <summary>
        /// Gets or sets the referrer.
        /// </summary>
        /// <value>The referrer.</value>
        public string Referer
        {
            get => GetHeaderValue(HeaderNames.Referer);
            set => RequestHeaders[HeaderNames.Referer] = value;
        }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host
        {
            get => GetHeaderValue(HeaderNames.Host);
            set => RequestHeaders[HeaderNames.Host] = value;
        }

        public Dictionary<string, string> RequestHeaders { get; private set; }

        public string RequestContentType { get; set; }

        public string RequestContent { get; set; }

        public bool BufferContent { get; set; }

        public bool LogErrorResponseBody { get; set; }

        public bool EnableKeepAlive { get; set; }

        public CacheMode CacheMode { get; set; }

        public TimeSpan CacheLength { get; set; }

        public bool EnableDefaultUserAgent { get; set; }

        private string GetHeaderValue(string name)
        {
            RequestHeaders.TryGetValue(name, out var value);

            return value;
        }
    }
}
