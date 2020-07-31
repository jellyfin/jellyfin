#pragma warning disable CS1591

namespace MediaBrowser.Model.Services
{
    public interface IHttpRequest : IRequest
    {
        /// <summary>
        /// Gets the HTTP Verb.
        /// </summary>
        string HttpMethod { get; }

        /// <summary>
        /// Gets the value of the Accept HTTP Request Header.
        /// </summary>
        string Accept { get; }
    }
}
