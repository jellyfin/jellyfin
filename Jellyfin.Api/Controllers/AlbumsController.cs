using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The albums controller.
    /// </summary>
    [Route("")]
    public class AlbumsController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumsController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public AlbumsController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Finds albums similar to a given album.
        /// </summary>
        /// <param name="albumId">The album id.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <param name="excludeArtistIds">Optional. Ids of artists to exclude.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <response code="200">Similar albums returned.</response>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with similar albums.</returns>
        [HttpGet("Albums/{albumId}/Similar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetSimilarAlbums(
            [FromRoute, Required] string albumId,
            [FromQuery] Guid? userId,
            [FromQuery] string? excludeArtistIds,
            [FromQuery] int? limit)
        {
            var dtoOptions = new DtoOptions().AddClientFields(Request);

            return SimilarItemsHelper.GetSimilarItemsResult(
                dtoOptions,
                _userManager,
                _libraryManager,
                _dtoService,
                userId,
                albumId,
                excludeArtistIds,
                limit,
                new[] { typeof(MusicAlbum) },
                GetAlbumSimilarityScore);
        }

        /// <summary>
        /// Finds artists similar to a given artist.
        /// </summary>
        /// <param name="artistId">The artist id.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <param name="excludeArtistIds">Optional. Ids of artists to exclude.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <response code="200">Similar artists returned.</response>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with similar artists.</returns>
        [HttpGet("Artists/{artistId}/Similar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetSimilarArtists(
            [FromRoute, Required] string artistId,
            [FromQuery] Guid? userId,
            [FromQuery] string? excludeArtistIds,
            [FromQuery] int? limit)
        {
            var dtoOptions = new DtoOptions().AddClientFields(Request);

            return SimilarItemsHelper.GetSimilarItemsResult(
                dtoOptions,
                _userManager,
                _libraryManager,
                _dtoService,
                userId,
                artistId,
                excludeArtistIds,
                limit,
                new[] { typeof(MusicArtist) },
                SimilarItemsHelper.GetSimiliarityScore);
        }

        /// <summary>
        /// Gets a similairty score of two albums.
        /// </summary>
        /// <param name="item1">The first item.</param>
        /// <param name="item1People">The item1 people.</param>
        /// <param name="allPeople">All people.</param>
        /// <param name="item2">The second item.</param>
        /// <returns>System.Int32.</returns>
        private int GetAlbumSimilarityScore(BaseItem item1, List<PersonInfo> item1People, List<PersonInfo> allPeople, BaseItem item2)
        {
            var points = SimilarItemsHelper.GetSimiliarityScore(item1, item1People, allPeople, item2);

            var album1 = (MusicAlbum)item1;
            var album2 = (MusicAlbum)item2;

            var artists1 = album1
                .GetAllArtists()
                .DistinctNames()
                .ToList();

            var artists2 = new HashSet<string>(
                album2.GetAllArtists().DistinctNames(),
                StringComparer.OrdinalIgnoreCase);

            return points + artists1.Where(artists2.Contains).Sum(i => 5);
        }
    }
}
