using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpRequestOptions
    /// </summary>
    public class HttpRequestOptions
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        public DecompressionMethods? DecompressionMethod { get; set; }

        /// <summary>
        /// Gets or sets the accept header.
        /// </summary>
        /// <value>The accept header.</value>
        public string AcceptHeader
        {
            get { return GetHeaderValue("Accept"); }
            set
            {
                RequestHeaders["Accept"] = value;
            }
        }
        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        /// <value>The cancellation token.</value>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the resource pool.
        /// </summary>
        /// <value>The resource pool.</value>
        public SemaphoreSlim ResourcePool { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>The user agent.</value>
        public string UserAgent
        {
            get { return GetHeaderValue("User-Agent"); }
            set
            {
                RequestHeaders["User-Agent"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the referrer.
        /// </summary>
        /// <value>The referrer.</value>
        public string Referer { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the progress.
        /// </summary>
        /// <value>The progress.</value>
        public IProgress<double> Progress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP compression].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP compression]; otherwise, <c>false</c>.</value>
        public bool EnableHttpCompression { get; set; }

        public Dictionary<string, string> RequestHeaders { get; private set; }

        public string RequestContentType { get; set; }

        public string RequestContent { get; set; }
        public byte[] RequestContentBytes { get; set; }

        public bool BufferContent { get; set; }

        public bool LogRequest { get; set; }
        public bool LogErrors { get; set; }

        public bool LogErrorResponseBody { get; set; }
        public bool EnableKeepAlive { get; set; }

        public CacheMode CacheMode { get; set; }
        public TimeSpan CacheLength { get; set; }

        public int TimeoutMs { get; set; }
        public bool PreferIpv4 { get; set; }

        private string GetHeaderValue(string name)
        {
            string value;

            RequestHeaders.TryGetValue(name, out value);

            return value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestOptions"/> class.
        /// </summary>
        public HttpRequestOptions()
        {
            EnableHttpCompression = true;
            BufferContent = true;

            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            LogRequest = true;
            LogErrors = true;
            CacheMode = CacheMode.None;

            TimeoutMs = 20000;
        }

        public void SetPostData(IDictionary<string,string> values)
        {
            var strings = values.Keys.Select(key => string.Format("{0}={1}", key, values[key]));
            var postContent = string.Join("&", strings.ToArray());

            RequestContent = postContent;
            RequestContentType = "application/x-www-form-urlencoded";
        }
    }

    public enum CacheMode
    {
        None = 0,
        Unconditional = 1
    }
}
