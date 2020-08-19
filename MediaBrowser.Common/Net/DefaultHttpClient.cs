using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Default http client.
    /// </summary>
    public class DefaultHttpClient
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHttpClient" /> class.
        /// </summary>
        /// <param name="httpClient">Instance of httpclient.</param>
        public DefaultHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Make GET request.
        /// </summary>
        /// <param name="url">Url to request.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>.</returns>
        public Task<HttpResponseMessage> GetAsync(Uri url)
        {
            return _httpClient.GetAsync(url);
        }

        /// <summary>
        /// Make GET request.
        /// </summary>
        /// <param name="url">Url to request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>.</returns>
        public Task<HttpResponseMessage> GetAsync(Uri url, CancellationToken cancellationToken)
        {
            return _httpClient.GetAsync(url, cancellationToken);
        }

        /// <summary>
        /// Get stream.
        /// </summary>
        /// <param name="url">Url to get stream from.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="Stream"/>.</returns>
        public Task<Stream> GetStreamAsync(Uri url)
        {
            return _httpClient.GetStreamAsync(url);
        }

        /// <summary>
        /// Send request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>.</returns>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage)
        {
            return _httpClient.SendAsync(requestMessage);
        }

        /// <summary>
        /// Send request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>.</returns>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(requestMessage, cancellationToken);
        }
    }
}
