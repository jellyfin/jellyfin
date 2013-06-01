using System;
using System.Net.Http;

namespace MediaBrowser.Common.Implementations.HttpClientManager
{
    /// <summary>
    /// Class HttpClientInfo
    /// </summary>
    public class HttpClientInfo
    {
        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        public HttpClient HttpClient { get; set; }
        /// <summary>
        /// Gets or sets the last timeout.
        /// </summary>
        /// <value>The last timeout.</value>
        public DateTime LastTimeout { get; set; }
    }
}
