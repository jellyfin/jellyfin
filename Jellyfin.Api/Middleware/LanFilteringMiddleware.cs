using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Validates the LAN host IP based on application configuration.
/// </summary>
public class LanFilteringMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanFilteringMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    public LanFilteringMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Executes the middleware action.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="networkManager">The network manager.</param>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The async task.</returns>
    public async Task Invoke(HttpContext httpContext, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager, ILogger<LanFilteringMiddleware> logger)
    {
        if (serverConfigurationManager.GetNetworkConfiguration().EnableRemoteAccess)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var host = httpContext.GetNormalizedRemoteIP();
        if (!networkManager.IsInLocalNetwork(host))
        {
            // No access from network, respond with 503 instead of 200.
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            logger.LogWarning("Attempted connection from {Host} but it is not in the configured local network and remote access is disabled", host);
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
