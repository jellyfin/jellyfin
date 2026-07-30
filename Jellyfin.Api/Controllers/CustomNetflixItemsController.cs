#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

[Authorize]
[Route("CustomNetflix/v1/items")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixItemsController : BaseJellyfinApiController
{
    private readonly ICustomNetflixAutoplayService _autoplayService;
    private readonly ICustomNetflixSegmentService _segmentService;
    private readonly ICustomNetflixItemDetailsService _itemDetailsService;

    public CustomNetflixItemsController(
        ICustomNetflixAutoplayService autoplayService,
        ICustomNetflixSegmentService segmentService,
        ICustomNetflixItemDetailsService itemDetailsService)
    {
        _autoplayService = autoplayService;
        _segmentService = segmentService;
        _itemDetailsService = itemDetailsService;
    }

    [HttpGet("segments/coverage")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult<CustomNetflixMediaSegmentCoverageDto>> GetSegmentCoverage(
        CancellationToken cancellationToken)
        => Ok(await _segmentService.GetCoverageAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet("{itemId:guid}/details")]
    public async Task<ActionResult<CustomNetflixItemDetailsDto>> GetItemDetails(
        [FromRoute] Guid itemId,
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await _itemDetailsService.GetItemDetailsAsync(
            User.GetUserId(),
            profileId,
            itemId,
            cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{itemId:guid}/next-episode")]
    public async Task<ActionResult<CustomNetflixNextEpisodeDto>> GetNextEpisode(
        [FromRoute] Guid itemId,
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await _autoplayService.GetNextEpisodeAsync(User.GetUserId(), profileId, itemId, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPut("{itemId:guid}/segments/manual")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult<CustomNetflixMediaSegmentsResponseDto>> ReplaceManualSegments(
        [FromRoute] Guid itemId,
        [FromBody] CustomNetflixManualMediaSegmentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _segmentService.ReplaceManualSegmentsAsync(User.GetUserId(), itemId, request, cancellationToken).ConfigureAwait(false);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{itemId:guid}/segments/manual")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult> DeleteManualSegments(
        [FromRoute] Guid itemId,
        [FromQuery] string? types,
        CancellationToken cancellationToken)
    {
        var requestedTypes = string.IsNullOrWhiteSpace(types) ? null : new[] { types };
        var deleted = await _segmentService.DeleteManualSegmentsAsync(User.GetUserId(), itemId, requestedTypes, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }
}
