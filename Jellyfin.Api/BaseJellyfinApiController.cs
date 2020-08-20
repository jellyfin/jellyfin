using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Base api controller for the API setting a default route.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces(
        MediaTypeNames.Application.Json,
        MediaTypeNames.Application.Json + "; profile=\"CamelCase\"",
        MediaTypeNames.Application.Json + "; profile=\"PascalCase\"")]
    public class BaseJellyfinApiController : ControllerBase
    {
    }
}
