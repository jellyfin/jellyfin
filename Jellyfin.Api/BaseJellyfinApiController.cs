using System.Net.Mime;
using Jellyfin.Extensions.Json;
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
        JsonDefaults.CamelCaseMediaType,
        JsonDefaults.PascalCaseMediaType)]
    public class BaseJellyfinApiController : ControllerBase
    {
    }
}
