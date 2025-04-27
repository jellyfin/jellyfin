using System.Net;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Validates the IP of requests coming from local networks wrt. remote access.
/// </summary>
public class IPBasedAccessValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IPBasedAccessValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IPBasedAccessValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger to log to.</param>
    public IPBasedAccessValidationMiddleware(RequestDelegate next, ILogger<IPBasedAccessValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware action.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="networkManager">The network manager.</param>
    /// <returns>The async task.</returns>
    public async Task Invoke(HttpContext httpContext, INetworkManager networkManager)
    {
        if (httpContext.IsLocal())
        {
            // Accessing from the same machine as the server.
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var remoteIP = httpContext.GetNormalizedRemoteIP();

        var result = networkManager.ShouldAllowServerAccess(remoteIP);
        if (result != RemoteAccessPolicyResult.Allow)
        {
            // No access from network, respond with 503 instead of 200.
            _logger.LogWarning(
                "Blocking request to {Path} by {RemoteIP} due to IP filtering rule, reason: {Reason}",
                // url-encode to block log injection
                HttpUtility.UrlEncode(httpContext.Request.Path),
                remoteIP,
                result);
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
