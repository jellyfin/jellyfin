using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Redirect requests to robots.txt to web/robots.txt.
/// </summary>
public class RobotsRedirectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RobotsRedirectionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RobotsRedirectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public RobotsRedirectionMiddleware(
        RequestDelegate next,
        ILogger<RobotsRedirectionMiddleware> logger)
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
        if (httpContext.Request.Path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Redirecting robots.txt request to web/robots.txt");
            httpContext.Response.Redirect("web/robots.txt");
            return;
        }

        await _next(httpContext).ConfigureAwait(false);
    }
}
