using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Validates the IP of requests coming from local networks wrt. remote access.
/// </summary>
public class IPBasedAccessValidationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="IPBasedAccessValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    public IPBasedAccessValidationMiddleware(RequestDelegate next)
    {
        _next = next;
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
            // Running locally.
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var remoteIP = httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;

        if (!networkManager.HasRemoteAccess(remoteIP))
        {
            // No access from network, respond with 503 instead of 200.
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
