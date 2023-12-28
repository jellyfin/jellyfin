using System;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Middleware;

/// <summary>
/// Shows a custom message during server startup.
/// </summary>
public class ServerStartupMessageMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerStartupMessageMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    public ServerStartupMessageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Executes the middleware action.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="serverApplicationHost">The server application host.</param>
    /// <param name="localizationManager">The localization manager.</param>
    /// <returns>The async task.</returns>
    public async Task Invoke(
        HttpContext httpContext,
        IServerApplicationHost serverApplicationHost,
        ILocalizationManager localizationManager)
    {
        if (serverApplicationHost.CoreStartupHasCompleted
            || httpContext.Request.Path.Equals("/system/ping", StringComparison.OrdinalIgnoreCase))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var message = localizationManager.GetLocalizedString("StartupEmbyServerIsLoading");
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        await httpContext.Response.WriteAsync(message, httpContext.RequestAborted).ConfigureAwait(false);
    }
}
