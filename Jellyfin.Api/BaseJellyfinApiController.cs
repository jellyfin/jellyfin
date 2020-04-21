using Jellyfin.Api.Models.ExceptionDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Base api controller for the API setting a default route.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [ProducesResponseType(typeof(ExceptionDto), StatusCodes.Status500InternalServerError)]
    public class BaseJellyfinApiController : ControllerBase
    {
    }
}
