#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

[Authorize]
[Route("CustomNetflix/v1/profiles/{profileId:guid}")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixPlaybackController : BaseJellyfinApiController
{
    private readonly ICustomNetflixWatchProgressService _watchProgressService;
    private readonly ICustomNetflixWatchHistoryService _watchHistoryService;
    private readonly ICustomNetflixActiveProfileService _activeProfileService;
    private readonly ICustomNetflixAutoplayService _autoplayService;

    public CustomNetflixPlaybackController(
        ICustomNetflixWatchProgressService watchProgressService,
        ICustomNetflixWatchHistoryService watchHistoryService,
        ICustomNetflixActiveProfileService activeProfileService,
        ICustomNetflixAutoplayService autoplayService)
    {
        _watchProgressService = watchProgressService;
        _watchHistoryService = watchHistoryService;
        _activeProfileService = activeProfileService;
        _autoplayService = autoplayService;
    }

    [HttpGet("continue-watching")]
    public async Task<ActionResult> GetContinueWatching(
        [FromRoute] Guid profileId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? 20 : limit;
        var items = await _watchProgressService.GetContinueWatchingAsync(User.GetUserId(), profileId, limit, cancellationToken).ConfigureAwait(false);
        return new OkObjectResult(new { Items = items });
    }

    [HttpGet("history")]
    public async Task<ActionResult> GetHistory(
        [FromRoute] Guid profileId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var items = await _watchHistoryService.GetHistoryAsync(User.GetUserId(), profileId, limit, cancellationToken).ConfigureAwait(false);
        return new OkObjectResult(new { Items = items });
    }

    [HttpPost("progress")]
    public async Task<ActionResult<CustomNetflixWatchProgressDto>> ReportProgress(
        [FromRoute] Guid profileId,
        [FromBody] CustomNetflixWatchProgressReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var progress = await _watchProgressService.ReportProgressAsync(User.GetUserId(), profileId, request, cancellationToken).ConfigureAwait(false);
        return progress is null ? NotFound() : Ok(progress);
    }

    [HttpPost("progress/batch")]
    public async Task<ActionResult<CustomNetflixWatchProgressBatchResponse>> GetProgressForItems(
        [FromRoute] Guid profileId,
        [FromBody] CustomNetflixWatchProgressBatchRequest request,
        CancellationToken cancellationToken)
    {
        var progress = await _watchProgressService.GetProgressForItemsAsync(User.GetUserId(), profileId, request.ItemIds, cancellationToken).ConfigureAwait(false);
        return Ok(new CustomNetflixWatchProgressBatchResponse { Items = progress });
    }

    [HttpPost("items/{itemId:guid}/played")]
    public async Task<ActionResult<CustomNetflixWatchProgressDto>> SetPlayed(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        [FromBody] CustomNetflixMarkPlayedRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var progress = await _watchProgressService.SetPlayedAsync(User.GetUserId(), profileId, itemId, request.Played, cancellationToken).ConfigureAwait(false);
        return progress is null ? NotFound() : Ok(progress);
    }

    [HttpDelete("continue-watching/{itemId:guid}")]
    public async Task<ActionResult> HideFromContinueWatching(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var hidden = await _watchProgressService.HideFromContinueWatchingAsync(User.GetUserId(), profileId, itemId, cancellationToken).ConfigureAwait(false);
        return hidden ? NoContent() : NotFound();
    }

    [HttpPost("autoplay/still-watching/confirm")]
    public async Task<ActionResult<CustomNetflixStillWatchingConfirmationDto>> ConfirmStillWatching(
        [FromRoute] Guid profileId,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var result = await _autoplayService.ConfirmStillWatchingAsync(
            User.GetUserId(),
            profileId,
            cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<bool> HasConsistentActiveProfileAsync(Guid profileId, CancellationToken cancellationToken)
        => await _activeProfileService.GetActiveProfileForWriteAsync(
            User.GetUserId(),
            User.GetToken(),
            profileId,
            cancellationToken).ConfigureAwait(false) is not null;

    private static ConflictObjectResult ActiveProfileConflict()
        => new(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "CustomNetflix active profile changed",
            Detail = "The requested write profile is not the active profile for this authenticated session."
        });
}
