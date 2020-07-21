using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The videos controller.
    /// </summary>
    [Route("Videos")]
    public class VideosController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideosController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public VideosController(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets additional parts for a video.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <response code="200">Additional parts returned.</response>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the parts.</returns>
        [HttpGet("{itemId}/AdditionalParts")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetAdditionalPart([FromRoute] Guid itemId, [FromQuery] Guid? userId)
        {
            var user = userId.HasValue && !userId.Equals(Guid.Empty)
                ? _userManager.GetUserById(userId.Value)
                : null;

            var item = itemId.Equals(Guid.Empty)
                ? (!userId.Equals(Guid.Empty)
                    ? _libraryManager.GetUserRootFolder()
                    : _libraryManager.RootFolder)
                : _libraryManager.GetItemById(itemId);

            var dtoOptions = new DtoOptions();
            dtoOptions = dtoOptions.AddClientFields(Request);

            BaseItemDto[] items;
            if (item is Video video)
            {
                items = video.GetAdditionalParts()
                    .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, video))
                    .ToArray();
            }
            else
            {
                items = Array.Empty<BaseItemDto>();
            }

            var result = new QueryResult<BaseItemDto>
            {
                Items = items,
                TotalRecordCount = items.Length
            };

            return result;
        }

        /// <summary>
        /// Removes alternate video sources.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <response code="204">Alternate sources deleted.</response>
        /// <response code="404">Video not found.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success, or a <see cref="NotFoundResult"/> if the video doesn't exist.</returns>
        [HttpDelete("{itemId}/AlternateSources")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteAlternateSources([FromRoute] Guid itemId)
        {
            var video = (Video)_libraryManager.GetItemById(itemId);

            if (video == null)
            {
                return NotFound("The video either does not exist or the id does not belong to a video.");
            }

            foreach (var link in video.GetLinkedAlternateVersions())
            {
                link.SetPrimaryVersionId(null);
                link.LinkedAlternateVersions = Array.Empty<LinkedChild>();

                link.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
            }

            video.LinkedAlternateVersions = Array.Empty<LinkedChild>();
            video.SetPrimaryVersionId(null);
            video.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

            return NoContent();
        }

        /// <summary>
        /// Merges videos into a single record.
        /// </summary>
        /// <param name="itemIds">Item id list. This allows multiple, comma delimited.</param>
        /// <response code="204">Videos merged.</response>
        /// <response code="400">Supply at least 2 video ids.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success, or a <see cref="BadRequestResult"/> if less than two ids were supplied.</returns>
        [HttpPost("MergeVersions")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult MergeVersions([FromQuery] string? itemIds)
        {
            var items = RequestHelpers.Split(itemIds, ',', true)
                .Select(i => _libraryManager.GetItemById(i))
                .OfType<Video>()
                .OrderBy(i => i.Id)
                .ToList();

            if (items.Count < 2)
            {
                return BadRequest("Please supply at least two videos to merge.");
            }

            var videosWithVersions = items.Where(i => i.MediaSourceCount > 1).ToList();

            var primaryVersion = videosWithVersions.FirstOrDefault();
            if (primaryVersion == null)
            {
                primaryVersion = items
                    .OrderBy(i =>
                    {
                        if (i.Video3DFormat.HasValue || i.VideoType != VideoType.VideoFile)
                        {
                            return 1;
                        }

                        return 0;
                    })
                    .ThenByDescending(i => i.GetDefaultVideoStream()?.Width ?? 0)
                    .First();
            }

            var list = primaryVersion.LinkedAlternateVersions.ToList();

            foreach (var item in items.Where(i => i.Id != primaryVersion.Id))
            {
                item.SetPrimaryVersionId(primaryVersion.Id.ToString("N", CultureInfo.InvariantCulture));

                item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

                list.Add(new LinkedChild
                {
                    Path = item.Path,
                    ItemId = item.Id
                });

                foreach (var linkedItem in item.LinkedAlternateVersions)
                {
                    if (!list.Any(i => string.Equals(i.Path, linkedItem.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(linkedItem);
                    }
                }

                if (item.LinkedAlternateVersions.Length > 0)
                {
                    item.LinkedAlternateVersions = Array.Empty<LinkedChild>();
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
                }
            }

            primaryVersion.LinkedAlternateVersions = list.ToArray();
            primaryVersion.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
            return NoContent();
        }
    }
}
