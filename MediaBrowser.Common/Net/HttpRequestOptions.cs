using System;
using System.Collections.Generic;
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

            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
