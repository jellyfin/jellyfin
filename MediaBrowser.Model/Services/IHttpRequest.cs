namespace MediaBrowser.Model.Services
{
    public interface IHttpRequest : IRequest
    {
        /// <summary>
        /// The HTTP Verb
        /// </summary>
        string HttpMethod { get; }

        /// <summary>
        /// The value of the Accept HTTP Request Header
        /// </summary>
        string Accept { get; }
    }
}
