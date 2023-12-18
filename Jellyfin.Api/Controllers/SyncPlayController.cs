using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.SyncPlayDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The sync play controller.
/// </summary>
[Authorize(Policy = Policies.SyncPlayHasAccess)]
public class SyncPlayController : BaseJellyfinApiController
{
    private readonly ISessionManager _sessionManager;
    private readonly ISyncPlayManager _syncPlayManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayController"/> class.
    /// </summary>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="syncPlayManager">Instance of the <see cref="ISyncPlayManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public SyncPlayController(
        ISessionManager sessionManager,
        ISyncPlayManager syncPlayManager,
        IUserManager userManager)
    {
        _sessionManager = sessionManager;
        _syncPlayManager = syncPlayManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Create a new SyncPlay group.
    /// </summary>
    /// <param name="requestData">The settings of the new group.</param>
    /// <response code="204">New group created.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("New")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayCreateGroup)]
    public async Task<ActionResult> SyncPlayCreateGroup(
        [FromBody, Required] NewGroupRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new NewGroupRequest(requestData.GroupName);
        _syncPlayManager.NewGroup(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Join an existing SyncPlay group.
    /// </summary>
    /// <param name="requestData">The group to join.</param>
    /// <response code="204">Group join successful.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayJoinGroup)]
    public async Task<ActionResult> SyncPlayJoinGroup(
        [FromBody, Required] JoinGroupRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new JoinGroupRequest(requestData.GroupId);
        _syncPlayManager.JoinGroup(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Leave the joined SyncPlay group.
    /// </summary>
    /// <response code="204">Group leave successful.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayLeaveGroup()
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new LeaveGroupRequest();
        _syncPlayManager.LeaveGroup(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Gets all SyncPlay groups.
    /// </summary>
    /// <response code="200">Groups returned.</response>
    /// <returns>An <see cref="IEnumerable{GroupInfoView}"/> containing the available SyncPlay groups.</returns>
    [HttpGet("List")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.SyncPlayJoinGroup)]
    public async Task<ActionResult<IEnumerable<GroupInfoDto>>> SyncPlayGetGroups()
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new ListGroupsRequest();
        return Ok(_syncPlayManager.ListGroups(currentSession, syncPlayRequest).AsEnumerable());
    }

    /// <summary>
    /// Request to set new playlist in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new playlist to play in the group.</param>
    /// <response code="204">Queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("SetNewQueue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySetNewQueue(
        [FromBody, Required] PlayRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new PlayGroupRequest(
            requestData.PlayingQueue,
            requestData.PlayingItemPosition,
            requestData.StartPositionTicks);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to change playlist item in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new item to play.</param>
    /// <response code="204">Queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("SetPlaylistItem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySetPlaylistItem(
        [FromBody, Required] SetPlaylistItemRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new SetPlaylistItemGroupRequest(requestData.PlaylistItemId);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to remove items from the playlist in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The items to remove.</param>
    /// <response code="204">Queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("RemoveFromPlaylist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayRemoveFromPlaylist(
        [FromBody, Required] RemoveFromPlaylistRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new RemoveFromPlaylistGroupRequest(requestData.PlaylistItemIds, requestData.ClearPlaylist, requestData.ClearPlayingItem);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to move an item in the playlist in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new position for the item.</param>
    /// <response code="204">Queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("MovePlaylistItem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayMovePlaylistItem(
        [FromBody, Required] MovePlaylistItemRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new MovePlaylistItemGroupRequest(requestData.PlaylistItemId, requestData.NewIndex);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to queue items to the playlist of a SyncPlay group.
    /// </summary>
    /// <param name="requestData">The items to add.</param>
    /// <response code="204">Queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Queue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayQueue(
        [FromBody, Required] QueueRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new QueueGroupRequest(requestData.ItemIds, requestData.Mode);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request unpause in SyncPlay group.
    /// </summary>
    /// <response code="204">Unpause update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Unpause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayUnpause()
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new UnpauseGroupRequest();
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request pause in SyncPlay group.
    /// </summary>
    /// <response code="204">Pause update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayPause()
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new PauseGroupRequest();
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request stop in SyncPlay group.
    /// </summary>
    /// <response code="204">Stop update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayStop()
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new StopGroupRequest();
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request seek in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new playback position.</param>
    /// <response code="204">Seek update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Seek")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySeek(
        [FromBody, Required] SeekRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new SeekGroupRequest(requestData.PositionTicks);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Notify SyncPlay group that member is buffering.
    /// </summary>
    /// <param name="requestData">The player status.</param>
    /// <response code="204">Group state update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Buffering")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayBuffering(
        [FromBody, Required] BufferRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new BufferGroupRequest(
            requestData.When,
            requestData.PositionTicks,
            requestData.IsPlaying,
            requestData.PlaylistItemId);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Notify SyncPlay group that member is ready for playback.
    /// </summary>
    /// <param name="requestData">The player status.</param>
    /// <response code="204">Group state update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayReady(
        [FromBody, Required] ReadyRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new ReadyGroupRequest(
            requestData.When,
            requestData.PositionTicks,
            requestData.IsPlaying,
            requestData.PlaylistItemId);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request SyncPlay group to ignore member during group-wait.
    /// </summary>
    /// <param name="requestData">The settings to set.</param>
    /// <response code="204">Member state updated.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("SetIgnoreWait")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySetIgnoreWait(
        [FromBody, Required] IgnoreWaitRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new IgnoreWaitGroupRequest(requestData.IgnoreWait);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request next item in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The current item information.</param>
    /// <response code="204">Next item update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("NextItem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayNextItem(
        [FromBody, Required] NextItemRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new NextItemGroupRequest(requestData.PlaylistItemId);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request previous item in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The current item information.</param>
    /// <response code="204">Previous item update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("PreviousItem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlayPreviousItem(
        [FromBody, Required] PreviousItemRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new PreviousItemGroupRequest(requestData.PlaylistItemId);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to set repeat mode in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new repeat mode.</param>
    /// <response code="204">Play queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("SetRepeatMode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySetRepeatMode(
        [FromBody, Required] SetRepeatModeRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new SetRepeatModeGroupRequest(requestData.Mode);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Request to set shuffle mode in SyncPlay group.
    /// </summary>
    /// <param name="requestData">The new shuffle mode.</param>
    /// <response code="204">Play queue update sent to all group members.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("SetShuffleMode")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.SyncPlayIsInGroup)]
    public async Task<ActionResult> SyncPlaySetShuffleMode(
        [FromBody, Required] SetShuffleModeRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new SetShuffleModeGroupRequest(requestData.Mode);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Update session ping.
    /// </summary>
    /// <param name="requestData">The new ping.</param>
    /// <response code="204">Ping updated.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Ping")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SyncPlayPing(
        [FromBody, Required] PingRequestDto requestData)
    {
        var currentSession = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        var syncPlayRequest = new PingGroupRequest(requestData.Ping);
        _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
        return NoContent();
    }
}
