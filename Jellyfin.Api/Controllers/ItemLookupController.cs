using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Item lookup controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class ItemLookupController : BaseJellyfinApiController
    {
        private readonly IProviderManager _providerManager;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<ItemLookupController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLookupController"/> class.
        /// </summary>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ItemLookupController}"/> interface.</param>
        public ItemLookupController(
            IProviderManager providerManager,
            IServerConfigurationManager serverConfigurationManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogger<ItemLookupController> logger)
        {
            _providerManager = providerManager;
            _appPaths = serverConfigurationManager.ApplicationPaths;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <summary>
        /// Get the item's external id info.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">External id info retrieved.</response>
        /// <response code="404">Item not found.</response>
        /// <returns>List of external id info.</returns>
        [HttpGet("Items/{itemId}/ExternalIdInfos")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<ExternalIdInfo>> GetExternalIdInfos([FromRoute, Required] Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(_providerManager.GetExternalIdInfos(item));
        }

        /// <summary>
        /// Get movie remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Movie remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Movie")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetMovieRemoteSearchResults([FromBody, Required] RemoteSearchQuery<MovieInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<Movie, MovieInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get trailer remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Trailer remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Trailer")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetTrailerRemoteSearchResults([FromBody, Required] RemoteSearchQuery<TrailerInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<Trailer, TrailerInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get music video remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Music video remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/MusicVideo")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetMusicVideoRemoteSearchResults([FromBody, Required] RemoteSearchQuery<MusicVideoInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<MusicVideo, MusicVideoInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get series remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Series remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Series")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetSeriesRemoteSearchResults([FromBody, Required] RemoteSearchQuery<SeriesInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<Series, SeriesInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get box set remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Box set remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/BoxSet")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetBoxSetRemoteSearchResults([FromBody, Required] RemoteSearchQuery<BoxSetInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<BoxSet, BoxSetInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get music artist remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Music artist remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/MusicArtist")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetMusicArtistRemoteSearchResults([FromBody, Required] RemoteSearchQuery<ArtistInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<MusicArtist, ArtistInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get music album remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Music album remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/MusicAlbum")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetMusicAlbumRemoteSearchResults([FromBody, Required] RemoteSearchQuery<AlbumInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<MusicAlbum, AlbumInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get person remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Person remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Person")]
        [Authorize(Policy = Policies.RequiresElevation)]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetPersonRemoteSearchResults([FromBody, Required] RemoteSearchQuery<PersonLookupInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<Person, PersonLookupInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Get book remote search.
        /// </summary>
        /// <param name="query">Remote search query.</param>
        /// <response code="200">Book remote search executed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="OkResult"/> containing the list of remote search results.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Book")]
        public async Task<ActionResult<IEnumerable<RemoteSearchResult>>> GetBookRemoteSearchResults([FromBody, Required] RemoteSearchQuery<BookInfo> query)
        {
            var results = await _providerManager.GetRemoteSearchResults<Book, BookInfo>(query, CancellationToken.None)
                .ConfigureAwait(false);
            return Ok(results);
        }

        /// <summary>
        /// Applies search criteria to an item and refreshes metadata.
        /// </summary>
        /// <param name="itemId">Item id.</param>
        /// <param name="searchResult">The remote search result.</param>
        /// <param name="replaceAllImages">Optional. Whether or not to replace all images. Default: True.</param>
        /// <response code="204">Item metadata refreshed.</response>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous operation to get the remote search results.
        /// The task result contains an <see cref="NoContentResult"/>.
        /// </returns>
        [HttpPost("Items/RemoteSearch/Apply/{itemId}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ApplySearchCriteria(
            [FromRoute, Required] Guid itemId,
            [FromBody, Required] RemoteSearchResult searchResult,
            [FromQuery] bool replaceAllImages = true)
        {
            var item = _libraryManager.GetItemById(itemId);
            _logger.LogInformation(
                "Setting provider id's to item {0}-{1}: {2}",
                item.Id,
                item.Name,
                JsonSerializer.Serialize(searchResult.ProviderIds));

            // Since the refresh process won't erase provider Ids, we need to set this explicitly now.
            item.ProviderIds = searchResult.ProviderIds;
            await _providerManager.RefreshFullItem(
                item,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = true,
                    ReplaceAllImages = replaceAllImages,
                    SearchResult = searchResult
                }, CancellationToken.None).ConfigureAwait(false);

            return NoContent();
        }
    }
}
