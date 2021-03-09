using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Base controller for the v2 API setting a default route.
    /// NOTE: This probably shouldn't be the place for the new API.
    /// </summary>
    [ApiController]
    [ApiVersion(ApiVersions.V2)] // This attribute can't be inherited. Have to declare it for every controller.
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseJellyfinV2ApiController : ControllerBase
    {
        /// <summary>
        /// Demo API v2 endpoint.
        /// </summary>
        /// <returns>A demo string.</returns>
        [HttpGet]
        public string Get() => "API Version 2";
    }
}
