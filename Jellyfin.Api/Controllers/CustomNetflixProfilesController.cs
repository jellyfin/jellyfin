#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

[Authorize]
[Route("CustomNetflix/v1/profiles")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixProfilesController : BaseJellyfinApiController
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixActiveProfileService _activeProfileService;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;

    public CustomNetflixProfilesController(
        ICustomNetflixProfileService profileService,
        ICustomNetflixActiveProfileService activeProfileService,
        IUserManager userManager,
        ISessionManager sessionManager)
    {
        _profileService = profileService;
        _activeProfileService = activeProfileService;
        _userManager = userManager;
        _sessionManager = sessionManager;
    }

    [HttpGet]
    public async Task<ActionResult> GetProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _profileService.GetProfilesAsync(User.GetUserId(), cancellationToken).ConfigureAwait(false);
        return new OkObjectResult(new { Profiles = profiles });
    }

    [HttpPost]
    public async Task<ActionResult<CustomNetflixProfileDto>> CreateProfile(
        [FromBody] CustomNetflixCreateProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profileService.CreateProfileAsync(User.GetUserId(), request, cancellationToken).ConfigureAwait(false);
            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (CustomNetflixProfileLimitExceededException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "CustomNetflix profile limit reached",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{profileId:guid}")]
    public async Task<ActionResult<CustomNetflixProfileDto>> UpdateProfile(
        [FromRoute] Guid profileId,
        [FromBody] CustomNetflixUpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profileService.UpdateProfileAsync(User.GetUserId(), profileId, request, cancellationToken).ConfigureAwait(false);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{profileId:guid}")]
    public async Task<ActionResult> DeleteProfile([FromRoute] Guid profileId, CancellationToken cancellationToken)
    {
        var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        if (session.NowPlayingItem is not null)
        {
            return ProfileChangeDuringPlaybackConflict();
        }

        RequestHelpers.BeginCustomNetflixProfileChange(session);
        try
        {
            var deleted = await _profileService.DeleteProfileAsync(User.GetUserId(), profileId, cancellationToken).ConfigureAwait(false);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<CustomNetflixActiveProfileDto>> GetActiveProfile(CancellationToken cancellationToken)
    {
        var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var generation = RequestHelpers.BeginCustomNetflixProfileResolution(session);
        var profile = await _activeProfileService.GetActiveProfileAsync(User.GetUserId(), User.GetToken(), cancellationToken).ConfigureAwait(false);
        RequestHelpers.TrySetCustomNetflixProfileResolution(
            session,
            generation,
            User.GetUserId(),
            User.GetToken(),
            CustomNetflixNativePlaystateSyncPolicy.ShouldSync(profile.Profile));
        return Ok(profile);
    }

    [HttpPut("active")]
    public async Task<ActionResult<CustomNetflixActiveProfileDto>> SetActiveProfile(
        [FromBody] CustomNetflixSetActiveProfileRequest request,
        CancellationToken cancellationToken)
    {
        var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        if (session.NowPlayingItem is not null)
        {
            return ProfileChangeDuringPlaybackConflict();
        }

        var generation = RequestHelpers.BeginCustomNetflixProfileChange(session);
        var profile = await _activeProfileService.SetActiveProfileAsync(User.GetUserId(), User.GetToken(), request.ProfileId, cancellationToken).ConfigureAwait(false);
        if (profile is not null)
        {
            if (!RequestHelpers.TrySetCustomNetflixProfileResolution(
                session,
                generation,
                User.GetUserId(),
                User.GetToken(),
                CustomNetflixNativePlaystateSyncPolicy.ShouldSync(profile.Profile)))
            {
                // Concurrent switches may complete out of order; force the next request to reload PostgreSQL.
                RequestHelpers.BeginCustomNetflixProfileChange(session);
            }
        }

        return profile is null ? NotFound() : Ok(profile);
    }

    private static ConflictObjectResult ProfileChangeDuringPlaybackConflict()
        => new(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "CustomNetflix profile is in use",
            Detail = "Stop the current playback before switching or deleting profiles."
        });
}
