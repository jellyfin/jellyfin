using Emby.Dlna;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Api.Attributes;

/// <inheritdoc />
public sealed class DlnaEnabledAttribute : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var serverConfigurationManager = context.HttpContext.RequestServices.GetRequiredService<IServerConfigurationManager>();

        var enabled = serverConfigurationManager.GetDlnaConfiguration().EnableServer;

        if (!enabled)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
