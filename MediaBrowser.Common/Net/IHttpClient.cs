using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IHttpClient
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> GetResponse(HttpRequestOptions options);

        /// <summary>
        /// Gets the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> Get(HttpRequestOptions options);

        /// <summary>
        /// Warning: Deprecated function,
        /// use 'Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, HttpMethod httpMethod, HttpStatusCode[] allowedStatusCodes);' instead
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod);

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="allowedStatusCodes">An array of status codes that should be treated as a success.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, HttpMethod httpMethod, HttpStatusCode[] allowedStatusCodes = null);

        /// <summary>
        /// Posts the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="allowedStatusCodes">An array of status codes that should be treated as a success.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> Post(HttpRequestOptions options, HttpStatusCode[] allowedStatusCodes = null);
    }
}
