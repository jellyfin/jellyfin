using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Removes /emby and /mediabrowser from requested route.
/// </summary>
public class LegacyEmbyRouteRewriteMiddleware
{
    private const string EmbyPath = "/emby";
    private const string MediabrowserPath = "/mediabrowser";

    private readonly RequestDelegate _next;
    private readonly ILogger<LegacyEmbyRouteRewriteMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyEmbyRouteRewriteMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public LegacyEmbyRouteRewriteMiddleware(
        RequestDelegate next,
        ILogger<LegacyEmbyRouteRewriteMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware action.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The async task.</returns>
    public async Task Invoke(HttpContext httpContext)
    {
        var localPath = httpContext.Request.Path.ToString();
        if (localPath.StartsWith(EmbyPath, StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Request.Path = localPath[EmbyPath.Length..];
            _logger.LogDebug("Removing {EmbyPath} from route.", EmbyPath);
        }
        else if (localPath.StartsWith(MediabrowserPath, StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Request.Path = localPath[MediabrowserPath.Length..];
            _logger.LogDebug("Removing {MediabrowserPath} from route.", MediabrowserPath);
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
