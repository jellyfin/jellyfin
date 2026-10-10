using System.Net;
using System.Net.Http;

namespace MediaBrowser.Common.Net;

/// <summary>
/// An <see cref="HttpRequestException"/> that retains the body of the failed response.
/// </summary>
/// <remarks>
/// Providers report why they refused a request in the response body, which
/// <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> discards.
/// </remarks>
public class HttpResponseBodyException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResponseBodyException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The status code of the failed response.</param>
    /// <param name="responseBody">The body of the failed response.</param>
    public HttpResponseBodyException(string message, HttpStatusCode? statusCode, string? responseBody)
        : base(message, null, statusCode)
    {
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the body of the failed response.
    /// </summary>
    public string? ResponseBody { get; }
}
