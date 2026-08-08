using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Short circuits an obsolete endpoint to return an error code.
/// </summary>
public class ObsoleteEndpointFilter : IAsyncActionFilter
{
    /// <inheritdoc/>
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var obsoleteResponse = new
        {
            error = "Endpoint Obsolete",
            message = "This endpoint has been manually disabled. You can revert this with the EnableObsoleteEndpoints setting."
        };

        context.Result = new ObjectResult(obsoleteResponse) { StatusCode = StatusCodes.Status410Gone };

        return Task.CompletedTask;
    }
}
