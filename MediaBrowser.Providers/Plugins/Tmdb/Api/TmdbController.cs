using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TMDbLib.Objects.General;

namespace MediaBrowser.Providers.Plugins.Tmdb.Api
{
    /// <summary>
    /// The TMDb API controller.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class TmdbController : ControllerBase
    {
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbController"/> class.
        /// </summary>
        /// <param name="tmdbClientManager">The TMDb client manager.</param>
        public TmdbController(TmdbClientManager tmdbClientManager)
        {
            _tmdbClientManager = tmdbClientManager;
        }

        /// <summary>
        /// Gets the TMDb image configuration options.
        /// </summary>
        /// <returns>The image portion of the TMDb client configuration.</returns>
        [HttpGet("ClientConfiguration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ConfigImageTypes> TmdbClientConfiguration()
        {
            return (await _tmdbClientManager.GetClientConfiguration().ConfigureAwait(false)).Images;
        }
    }
}
