using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    /// <summary>
    /// Controller for testing the encoded url.
    /// </summary>
    public class EncoderController : BaseJellyfinTestController
    {
        /// <summary>
        /// Tests the url decoding.
        /// </summary>
        /// <param name="params">Parameters to echo back in the response.</param>
        /// <returns>An <see cref="OkResult"/>.</returns>
        /// <response code="200">Information retrieved.</response>
        [HttpGet("UrlDecode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ContentResult TestUrlDecoding([FromQuery] Dictionary<string, string>? @params = null)
        {
            return new ContentResult()
            {
                Content = (@params is not null && @params.Count > 0)
                    ? string.Join("&", @params.Select(x => x.Key + "=" + x.Value))
                    : string.Empty,
                ContentType = "text/plain; charset=utf-8",
                StatusCode = 200
            };
        }

        /// <summary>
        /// Tests the url decoding.
        /// </summary>
        /// <param name="params">Parameters to echo back in the response.</param>
        /// <returns>An <see cref="OkResult"/>.</returns>
        /// <response code="200">Information retrieved.</response>
        [HttpGet("UrlArrayDecode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ContentResult TestUrlArrayDecoding([FromQuery] Dictionary<string, string[]>? @params = null)
        {
            return new ContentResult()
            {
                Content = (@params is not null && @params.Count > 0)
                    ? string.Join("&", @params.Select(x => x.Key + "=" + string.Join(',', x.Value)))
                    : string.Empty,
                ContentType = "text/plain; charset=utf-8",
                StatusCode = 200
            };
        }
    }
}
