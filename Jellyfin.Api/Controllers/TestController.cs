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
    [Route("Tests")]
    public class TestController : BaseJellyfinApiController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestController"/> class.
        /// </summary>
        public TestController()
        {
        }

        /// <summary>
        /// Tests the url decoding.
        /// </summary>
        /// <param name="params">Parameters to echo back in the response.</param>
        /// <returns>An <see cref="OkResult"/>.</returns>
        /// <response code="200">Information retrieved.</response>
        [HttpGet("Decoding", Name = "TestUrlDecoding")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult TestUrlDecoding([FromQuery]Dictionary<string, string>? @params = null)
        {
            if (@params != null && @params.Count > 0)
            {
                Response.Headers.Add("querystring", string.Join("&", @params.Select(x => x.Key + "=" + x.Value)));
            }

            return Ok();
        }
    }
}
