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
[Route("CustomNetflix/v1/profiles/{profileId:guid}/my-list")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixMyListController : BaseJellyfinApiController
{
    private readonly ICustomNetflixMyListService _myListService;
    private readonly ICustomNetflixActiveProfileService _activeProfileService;

    public CustomNetflixMyListController(
        ICustomNetflixMyListService myListService,
        ICustomNetflixActiveProfileService activeProfileService)
    {
        _myListService = myListService;
        _activeProfileService = activeProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<CustomNetflixMyListResponseDto>> GetMyList(
        [FromRoute] Guid profileId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var result = await _myListService.GetMyListAsync(User.GetUserId(), profileId, limit, cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<CustomNetflixMyListStatusDto>> GetStatus(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _myListService.GetStatusAsync(User.GetUserId(), profileId, itemId, cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{itemId:guid}")]
    public async Task<ActionResult<CustomNetflixMyListStatusDto>> Add(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var result = await _myListService.AddAsync(User.GetUserId(), profileId, itemId, cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<ActionResult<CustomNetflixMyListStatusDto>> Remove(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await HasConsistentActiveProfileAsync(profileId, cancellationToken).ConfigureAwait(false))
        {
            return ActiveProfileConflict();
        }

        var result = await _myListService.RemoveAsync(User.GetUserId(), profileId, itemId, cancellationToken).ConfigureAwait(false);
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
