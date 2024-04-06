using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.Requests;
using Jellyfin.Api.Services;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The videos controller.
/// </summary>
public class VideosController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly IVideoService _videoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideosController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="videoService">Instance of the <see cref="IVideoService"/>.</param>
    public VideosController(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService, IVideoService videoService)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _videoService = videoService;
    }

    /// <summary>
    /// Gets additional parts for a video.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <response code="200">Additional parts returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the parts.</returns>
    [HttpGet("{itemId}/AdditionalParts")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetAdditionalPart([FromRoute, Required] Guid itemId, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var item = itemId.IsEmpty()
            ? (userId.IsNullOrEmpty()
                ? _libraryManager.RootFolder
                : _libraryManager.GetUserRootFolder())
            : _libraryManager.GetItemById(itemId);

        var dtoOptions = new DtoOptions();
        dtoOptions = dtoOptions.AddClientFields(User);

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

        var result = new QueryResult<BaseItemDto>(items);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAlternateSources([FromRoute, Required] Guid itemId)
    {
        var video = (Video)_libraryManager.GetItemById(itemId);

        if (video is null)
        {
            return NotFound("The video either does not exist or the id does not belong to a video.");
        }

        if (video.LinkedAlternateVersions.Length == 0)
        {
            video = (Video?)_libraryManager.GetItemById(video.PrimaryVersionId);
        }

        if (video is null)
        {
            return NotFound();
        }

        foreach (var link in video.GetLinkedAlternateVersions())
        {
            link.SetPrimaryVersionId(null);
            link.LinkedAlternateVersions = Array.Empty<LinkedChild>();

            await link.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        video.LinkedAlternateVersions = Array.Empty<LinkedChild>();
        video.SetPrimaryVersionId(null);
        await video.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Merges videos into a single record.
    /// </summary>
    /// <param name="ids">Item id list. This allows multiple, comma delimited.</param>
    /// <response code="204">Videos merged.</response>
    /// <response code="400">Supply at least 2 video ids.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success, or a <see cref="BadRequestResult"/> if less than two ids were supplied.</returns>
    [HttpPost("MergeVersions")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> MergeVersions([FromQuery, Required, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] ids)
    {
        var items = ids
            .Select(i => _libraryManager.GetItemById(i))
            .OfType<Video>()
            .OrderBy(i => i.Id)
            .ToList();

        if (items.Count < 2)
        {
            return BadRequest("Please supply at least two videos to merge.");
        }

        var primaryVersion = items.FirstOrDefault(i => i.MediaSourceCount > 1 && string.IsNullOrEmpty(i.PrimaryVersionId));
        if (primaryVersion is null)
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

        var alternateVersionsOfPrimary = primaryVersion.LinkedAlternateVersions.ToList();

        foreach (var item in items.Where(i => !i.Id.Equals(primaryVersion.Id)))
        {
            item.SetPrimaryVersionId(primaryVersion.Id.ToString("N", CultureInfo.InvariantCulture));

            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (!alternateVersionsOfPrimary.Any(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase)))
            {
                alternateVersionsOfPrimary.Add(new LinkedChild
                {
                    Path = item.Path,
                    ItemId = item.Id
                });
            }

            foreach (var linkedItem in item.LinkedAlternateVersions)
            {
                if (!alternateVersionsOfPrimary.Any(i => string.Equals(i.Path, linkedItem.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    alternateVersionsOfPrimary.Add(linkedItem);
                }
            }

            if (item.LinkedAlternateVersions.Length > 0)
            {
                item.LinkedAlternateVersions = Array.Empty<LinkedChild>();
                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }
        }

        primaryVersion.LinkedAlternateVersions = alternateVersionsOfPrimary.ToArray();
        await primaryVersion.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Gets a video stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="request">The query request details <see cref="VideoStreamRequest"/>.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream")]
    [HttpHead("{itemId}/stream", Name = "HeadVideoStream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesVideoFile]
    public async Task<ActionResult> GetVideoStream([FromRoute][Required] Guid itemId, [FromQuery] VideoStreamRequest request)
    {
        return await _videoService.GetVideoStreamAsync(itemId, request, Request, HttpContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a video stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="request">the request from query string <see cref="VideoStreamRequest"/>.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream.{request.container}")]
    [HttpHead("{itemId}/stream.{request.container}", Name = "HeadVideoStreamByContainer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesVideoFile]
    public async Task<ActionResult> GetVideoStreamByContainer([FromRoute, Required] Guid itemId, [FromQuery] VideoStreamRequest request)
    {
        return await _videoService.GetVideoStreamAsync(itemId, request, Request, HttpContext).ConfigureAwait(false);
    }
}
