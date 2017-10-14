using System;
using System.Net.Http;

namespace Emby.Server.Implementations.HttpClientManager
{
    /// <summary>
    /// Class HttpClientInfo
    /// </summary>
    public class HttpClientInfo
    {
        /// <summary>
        /// Gets or sets the last timeout.
        /// </summary>
        /// <value>The last timeout.</value>
        public DateTime LastTimeout { get; set; }
        public HttpClient HttpClient { get; set; }
    }
}
