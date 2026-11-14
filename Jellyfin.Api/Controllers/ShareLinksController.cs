using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.ShareLinks;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.ShareLinks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Controller for scoped, encrypted share links.
/// </summary>
[Authorize]
[Tags("ShareLinks")]
public class ShareLinksController : BaseJellyfinApiController
{
    private const string ShareLinkErrorHeader = "X-ShareLink-Error";

    private readonly IShareLinkTokenService _shareLinkTokenService;
    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IShareLinkPolicyService _shareLinkPolicyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinksController"/> class.
    /// </summary>
    /// <param name="shareLinkTokenService">Instance of the <see cref="IShareLinkTokenService"/> interface.</param>
    /// <param name="shareLinkRepository">Instance of the <see cref="IShareLinkRepository"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="shareLinkPolicyService">Instance of the <see cref="IShareLinkPolicyService"/> interface.</param>
    public ShareLinksController(
        IShareLinkTokenService shareLinkTokenService,
        IShareLinkRepository shareLinkRepository,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IShareLinkPolicyService shareLinkPolicyService)
    {
        _shareLinkTokenService = shareLinkTokenService;
        _shareLinkRepository = shareLinkRepository;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _shareLinkPolicyService = shareLinkPolicyService;
    }

    /// <summary>
    /// Creates a scoped share link for a media item.
    /// </summary>
    /// <param name="itemId">The id of the media item.</param>
    /// <param name="request">The share link creation request.</param>
    /// <response code="201">The share link was created.</response>
    /// <response code="400">The expiration or duration is missing or invalid.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">Share links are disabled or the user cannot play the item.</response>
    /// <response code="404">The media item was not found.</response>
    /// <returns>The created share link.</returns>
    [HttpPost("/Items/{itemId}/ShareLinks")]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShareLinkDto>> CreateShareLink(
        [FromRoute] Guid itemId,
        [FromBody] CreateShareLinkRequestDto request)
    {
        var userId = User.GetUserId();
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        // Effective policy: hard server-wide yes/no, otherwise the user's own choice with
        // the server-wide default as fallback.
        if (!_shareLinkPolicyService.IsSharingAllowed(user))
        {
            Response.Headers[ShareLinkErrorHeader] = ShareLinkCreateErrorCode.SharingDisabled.ToString();
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.IsVisible(user) || !user.HasPermission(PermissionKind.EnableMediaPlayback))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (request is null || (!request.ExpiresAt.HasValue && !request.DurationSeconds.HasValue))
        {
            Response.Headers[ShareLinkErrorHeader] = ShareLinkCreateErrorCode.ExpirationOrDurationRequired.ToString();
            return BadRequest();
        }

        var expiresAtUnixSeconds = request.ExpiresAt.HasValue
            ? ToUnixSeconds(request.ExpiresAt.Value)
            : (long?)null;

        var result = await _shareLinkTokenService.CreateAsync(
            userId,
            itemId,
            expiresAtUnixSeconds,
            request.DurationSeconds,
            HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.Succeeded || result.Token is null || result.ShareLink is null)
        {
            var errorCode = result.ErrorCode ?? ShareLinkCreateErrorCode.InvalidExpiration;
            Response.Headers[ShareLinkErrorHeader] = errorCode.ToString();
            return errorCode is ShareLinkCreateErrorCode.ExpirationOrDurationRequired
                or ShareLinkCreateErrorCode.InvalidExpiration
                or ShareLinkCreateErrorCode.InvalidDuration
                ? BadRequest()
                : StatusCode(StatusCodes.Status403Forbidden);
        }

        return StatusCode(StatusCodes.Status201Created, ToDto(result.ShareLink, result.Token));
    }

    /// <summary>
    /// Gets the share links created by a user.
    /// </summary>
    /// <param name="userId">The id of the user whose share links to list. Only administrators may list another user's links.</param>
    /// <response code="200">The share links were returned.</response>
    /// <response code="403">A non-administrator requested another user's share links.</response>
    /// <returns>The share links created by the target user.</returns>
    [HttpGet("/ShareLinks")]
    [ProducesResponseType(typeof(IEnumerable<ShareLinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ShareLinkDto>>> GetShareLinks([FromQuery] Guid? userId)
    {
        var targetUserId = GetTargetUserId(userId);
        if (targetUserId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var links = await _shareLinkRepository
            .ListByUserAsync(targetUserId.Value, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Ok(links.Select(link => ToDto(link, null)));
    }

    /// <summary>
    /// Gets the share links created by a user for a specific media item.
    /// </summary>
    /// <param name="itemId">The id of the media item.</param>
    /// <param name="userId">The id of the user whose share links to list. Only administrators may list another user's links.</param>
    /// <response code="200">The share links were returned.</response>
    /// <response code="403">A non-administrator requested another user's share links.</response>
    /// <returns>The share links created by the target user for the item.</returns>
    [HttpGet("/Items/{itemId}/ShareLinks")]
    [ProducesResponseType(typeof(IEnumerable<ShareLinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ShareLinkDto>>> GetItemShareLinks(
        [FromRoute] Guid itemId,
        [FromQuery] Guid? userId)
    {
        var targetUserId = GetTargetUserId(userId);
        if (targetUserId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var links = await _shareLinkRepository
            .ListByItemAsync(targetUserId.Value, itemId, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Ok(links.Select(link => ToDto(link, null)));
    }

    /// <summary>
    /// Revokes a share link.
    /// </summary>
    /// <param name="linkId">The token id (jti) of the share link.</param>
    /// <response code="204">The share link was revoked.</response>
    /// <response code="403">The user is neither the owner nor an administrator.</response>
    /// <response code="404">The share link was not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("/ShareLinks/{linkId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteShareLink([FromRoute] string linkId)
    {
        var link = await _shareLinkRepository
            .GetByIdAsync(linkId, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (link is null)
        {
            return NotFound();
        }

        var currentUserId = User.GetUserId();
        if (!link.UserId.Equals(currentUserId) && !User.IsInRole(UserRoles.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var revoked = await _shareLinkRepository
            .RevokeAsync(linkId, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return revoked ? NoContent() : NotFound();
    }

    private Guid? GetTargetUserId(Guid? requestedUserId)
    {
        var currentUserId = User.GetUserId();
        if (!requestedUserId.HasValue || requestedUserId.Value.Equals(currentUserId))
        {
            return currentUserId;
        }

        return User.IsInRole(UserRoles.Administrator) ? requestedUserId : null;
    }

    private ShareLinkDto ToDto(ShareLink link, string? token) => new()
    {
        Id = link.Id,
        ItemId = link.ItemId,
        Url = BuildShareUrl(link.ItemId, token),
        CreatedAt = link.CreatedAt,
        FirstAccessedAt = link.FirstAccessedAt,
        ExpiresAt = link.ExpiresAt,
        DurationSeconds = link.DurationSeconds,
        IsRevoked = link.IsRevoked
    };

    private string BuildShareUrl(Guid itemId, string? token)
    {
        // TODO: prefer the configured WanAddress (or public URL) instead of the incoming request host.
        var baseUrl = $"{Request.Scheme}://{Request.Host.ToUriComponent()}";

        // Route to the endpoint that can actually serve the item: audio items have no video
        // stream, so pointing them at /Videos would hand back an unusable link.
        var item = _libraryManager.GetItemById(itemId);
        var resource = item?.MediaType == MediaType.Audio
            ? "Audio"
            : "Videos";
        var streamUrl = $"{baseUrl}/{resource}/{itemId.ToString("N", CultureInfo.InvariantCulture)}/stream";

        // The raw token is never persisted, so list responses cannot replay it and fall back
        // to the bare stream URL. Only the create response carries the token.
        return string.IsNullOrEmpty(token)
            ? streamUrl
            : $"{streamUrl}?share_token={Uri.EscapeDataString(token)}";
    }

    private static long ToUnixSeconds(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utc).ToUnixTimeSeconds();
    }
}
