using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The session controller.
/// </summary>
[Route("")]
public class SessionController : BaseJellyfinApiController
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionController"/> class.
    /// </summary>
    /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
    /// <param name="userManager">Instance of <see cref="IUserManager"/> interface.</param>
    public SessionController(
        ISessionManager sessionManager,
        IUserManager userManager)
    {
        _sessionManager = sessionManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets a list of sessions.
    /// </summary>
    /// <param name="controllableByUserId">Filter by sessions that a given user is allowed to remote control.</param>
    /// <param name="deviceId">Filter by device Id.</param>
    /// <param name="activeWithinSeconds">Optional. Filter by sessions that were active in the last n seconds.</param>
    /// <response code="200">List of sessions returned.</response>
    /// <returns>An <see cref="IReadOnlyList{SessionInfoDto}"/> with the available sessions.</returns>
    [HttpGet("Sessions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<SessionInfoDto>> GetSessions(
        [FromQuery] Guid? controllableByUserId,
        [FromQuery] string? deviceId,
        [FromQuery] int? activeWithinSeconds)
    {
        Guid? controllableUserToCheck = controllableByUserId is null ? null : RequestHelpers.GetUserId(User, controllableByUserId);
        var result = _sessionManager.GetSessions(
            User.GetUserId(),
            deviceId,
            activeWithinSeconds,
            controllableUserToCheck,
            User.GetIsApiKey());

        return Ok(result);
    }

    /// <summary>
    /// Instructs a session to browse to an item or view.
    /// </summary>
    /// <param name="sessionId">The session Id.</param>
    /// <param name="itemType">The type of item to browse to.</param>
    /// <param name="itemId">The Id of the item.</param>
    /// <param name="itemName">The name of the item.</param>
    /// <response code="204">Instruction sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Viewing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DisplayContent(
        [FromRoute, Required] string sessionId,
        [FromQuery, Required] BaseItemKind itemType,
        [FromQuery, Required] string itemId,
        [FromQuery, Required] string itemName)
    {
        var command = new BrowseRequest
        {
            ItemId = itemId,
            ItemName = itemName,
            ItemType = itemType
        };

        await _sessionManager.SendBrowseCommand(
            await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false),
            sessionId,
            command,
            CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Instructs a session to play an item.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playCommand">The type of play command to issue (PlayNow, PlayNext, PlayLast). Clients who have not yet implemented play next and play last may play now.</param>
    /// <param name="itemIds">The ids of the items to play, comma delimited.</param>
    /// <param name="startPositionTicks">The starting position of the first item.</param>
    /// <param name="mediaSourceId">Optional. The media source id.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to play.</param>
    /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to play.</param>
    /// <param name="startIndex">Optional. The start index.</param>
    /// <response code="204">Instruction sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Playing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Play(
        [FromRoute, Required] string sessionId,
        [FromQuery, Required] PlayCommand playCommand,
        [FromQuery, Required, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] itemIds,
        [FromQuery] long? startPositionTicks,
        [FromQuery] string? mediaSourceId,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] int? startIndex)
    {
        var playRequest = new PlayRequest
        {
            ItemIds = itemIds,
            StartPositionTicks = startPositionTicks,
            PlayCommand = playCommand,
            MediaSourceId = mediaSourceId,
            AudioStreamIndex = audioStreamIndex,
            SubtitleStreamIndex = subtitleStreamIndex,
            StartIndex = startIndex
        };

        await _sessionManager.SendPlayCommand(
            await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false),
            sessionId,
            playRequest,
            CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Issues a playstate command to a client.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="command">The <see cref="PlaystateCommand"/>.</param>
    /// <param name="seekPositionTicks">The optional position ticks.</param>
    /// <param name="controllingUserId">The optional controlling user id.</param>
    /// <response code="204">Playstate command sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Playing/{command}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SendPlaystateCommand(
        [FromRoute, Required] string sessionId,
        [FromRoute, Required] PlaystateCommand command,
        [FromQuery] long? seekPositionTicks,
        [FromQuery] string? controllingUserId)
    {
        await _sessionManager.SendPlaystateCommand(
            await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false),
            sessionId,
            new PlaystateRequest()
            {
                Command = command,
                ControllingUserId = controllingUserId,
                SeekPositionTicks = seekPositionTicks,
            },
            CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Issues a system command to a client.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="command">The command to send.</param>
    /// <response code="204">System command sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/System/{command}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SendSystemCommand(
        [FromRoute, Required] string sessionId,
        [FromRoute, Required] GeneralCommandType command)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var generalCommand = new GeneralCommand
        {
            Name = command,
            ControllingUserId = currentSession.UserId
        };

        await _sessionManager.SendGeneralCommand(currentSession.Id, sessionId, generalCommand, CancellationToken.None).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Issues a general command to a client.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="command">The command to send.</param>
    /// <response code="204">General command sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Command/{command}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SendGeneralCommand(
        [FromRoute, Required] string sessionId,
        [FromRoute, Required] GeneralCommandType command)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);

        var generalCommand = new GeneralCommand
        {
            Name = command,
            ControllingUserId = currentSession.UserId
        };

        await _sessionManager.SendGeneralCommand(currentSession.Id, sessionId, generalCommand, CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Issues a full general command to a client.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="command">The <see cref="GeneralCommand"/>.</param>
    /// <response code="204">Full general command sent to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Command")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SendFullGeneralCommand(
        [FromRoute, Required] string sessionId,
        [FromBody, Required] GeneralCommand command)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(command);

        command.ControllingUserId = currentSession.UserId;

        await _sessionManager.SendGeneralCommand(
            currentSession.Id,
            sessionId,
            command,
            CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Issues a command to a client to display a message to the user.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="command">The <see cref="MessageCommand" /> object containing Header, Message Text, and TimeoutMs.</param>
    /// <response code="204">Message sent.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/Message")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SendMessageCommand(
        [FromRoute, Required] string sessionId,
        [FromBody, Required] MessageCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Header))
        {
            command.Header = "Message from Server";
        }

        await _sessionManager.SendMessageCommand(
            await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false),
            sessionId,
            command,
            CancellationToken.None)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Adds an additional user to a session.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="userId">The user id.</param>
    /// <response code="204">User added to session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/{sessionId}/User/{userId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult AddUserToSession(
        [FromRoute, Required] string sessionId,
        [FromRoute, Required] Guid userId)
    {
        _sessionManager.AddAdditionalUser(sessionId, userId);
        return NoContent();
    }

    /// <summary>
    /// Removes an additional user from a session.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="userId">The user id.</param>
    /// <response code="204">User removed from session.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Sessions/{sessionId}/User/{userId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult RemoveUserFromSession(
        [FromRoute, Required] string sessionId,
        [FromRoute, Required] Guid userId)
    {
        _sessionManager.RemoveAdditionalUser(sessionId, userId);
        return NoContent();
    }

    /// <summary>
    /// Updates capabilities for a device.
    /// </summary>
    /// <param name="id">The session id.</param>
    /// <param name="playableMediaTypes">A list of playable media types, comma delimited. Audio, Video, Book, Photo.</param>
    /// <param name="supportedCommands">A list of supported remote control commands, comma delimited.</param>
    /// <param name="supportsMediaControl">Determines whether media can be played remotely..</param>
    /// <param name="supportsPersistentIdentifier">Determines whether the device supports a unique identifier.</param>
    /// <response code="204">Capabilities posted.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Capabilities")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> PostCapabilities(
        [FromQuery] string? id,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] playableMediaTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] GeneralCommandType[] supportedCommands,
        [FromQuery] bool supportsMediaControl = false,
        [FromQuery] bool supportsPersistentIdentifier = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        }

        _sessionManager.ReportCapabilities(id, new ClientCapabilities
        {
            PlayableMediaTypes = playableMediaTypes,
            SupportedCommands = supportedCommands,
            SupportsMediaControl = supportsMediaControl,
            SupportsPersistentIdentifier = supportsPersistentIdentifier
        });
        return NoContent();
    }

    /// <summary>
    /// Updates capabilities for a device.
    /// </summary>
    /// <param name="id">The session id.</param>
    /// <param name="capabilities">The <see cref="ClientCapabilities"/>.</param>
    /// <response code="204">Capabilities updated.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Capabilities/Full")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> PostFullCapabilities(
        [FromQuery] string? id,
        [FromBody, Required] ClientCapabilitiesDto capabilities)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        }

        _sessionManager.ReportCapabilities(id, capabilities.ToClientCapabilities());

        return NoContent();
    }

    /// <summary>
    /// Reports that a session is viewing an item.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="itemId">The item id.</param>
    /// <response code="204">Session reported to server.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Viewing")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReportViewing(
        [FromQuery] string? sessionId,
        [FromQuery, Required] string? itemId)
    {
        string session = sessionId ?? await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);

        _sessionManager.ReportNowViewingItem(session, itemId);
        return NoContent();
    }

    /// <summary>
    /// Reports that a session has ended.
    /// </summary>
    /// <response code="204">Session end reported to server.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReportSessionEnded()
    {
        await _sessionManager.Logout(User.GetToken()).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Get all auth providers.
    /// </summary>
    /// <response code="200">Auth providers retrieved.</response>
    /// <returns>An <see cref="IEnumerable{NameIdPair}"/> with the auth providers.</returns>
    [HttpGet("Auth/Providers")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NameIdPair>> GetAuthProviders()
    {
        return _userManager.GetAuthenticationProviders();
    }

    /// <summary>
    /// Get all password reset providers.
    /// </summary>
    /// <response code="200">Password reset providers retrieved.</response>
    /// <returns>An <see cref="IEnumerable{NameIdPair}"/> with the password reset providers.</returns>
    [HttpGet("Auth/PasswordResetProviders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult<IEnumerable<NameIdPair>> GetPasswordResetProviders()
    {
        return _userManager.GetPasswordResetProviders();
    }
}
