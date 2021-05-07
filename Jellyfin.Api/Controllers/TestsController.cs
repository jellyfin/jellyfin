using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Controller for testing.
    /// </summary>
    public class TestsController : BaseJellyfinApiController
    {
        /// <summary>
        /// Tests the url decoding.
        /// </summary>
        /// <param name="params">Parameters to echo back in the response.</param>
        /// <returns>An <see cref="OkResult"/>.</returns>
        /// <response code="200">Information retrieved.</response>
        [HttpGet("UrlDecode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ContentResult TestUrlDecoding([FromQuery]Dictionary<string, string>? @params = null)
        {
            return new ContentResult()
            {
                Content = (@params != null && @params.Count > 0)
                    ? string.Join("&", @params.Select(x => x.Key + "=" + x.Value))
                    : string.Empty,
                ContentType = "text/plain; charset=utf-8",
                StatusCode = 200
            };
        }
    }
}
