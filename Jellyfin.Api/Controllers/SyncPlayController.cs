using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Model.SyncPlay.RequestBodies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The sync play controller.
    /// </summary>
    [Authorize(Policy = Policies.SyncPlayAccess)]
    public class SyncPlayController : BaseJellyfinApiController
    {
        private readonly ISessionManager _sessionManager;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly ISyncPlayManager _syncPlayManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayController"/> class.
        /// </summary>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="syncPlayManager">Instance of the <see cref="ISyncPlayManager"/> interface.</param>
        public SyncPlayController(
            ISessionManager sessionManager,
            IAuthorizationContext authorizationContext,
            ISyncPlayManager syncPlayManager)
        {
            _sessionManager = sessionManager;
            _authorizationContext = authorizationContext;
            _syncPlayManager = syncPlayManager;
        }

        /// <summary>
        /// Create a new SyncPlay group.
        /// </summary>
        /// <param name="requestData">The settings of the new group.</param>
        /// <response code="204">New group created.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("New")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [Authorize(Policy = Policies.SyncPlayCreateGroupAccess)]
        public ActionResult SyncPlayCreateGroup(
            [FromBody, Required] NewGroupRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlayJoinGroup(
            [FromBody, Required] JoinGroupRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlayLeaveGroup()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult<IEnumerable<GroupInfoDto>> SyncPlayGetGroups()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new ListGroupsRequest();
            return Ok(_syncPlayManager.ListGroups(currentSession, syncPlayRequest));
        }

        /// <summary>
        /// Request play in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The new playlist to play in the group.</param>
        /// <response code="204">Play request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Play")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayPlay(
            [FromBody, Required] PlayRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        /// <response code="204">Queue update request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("SetPlaylistItem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlaySetPlaylistItem(
            [FromBody, Required] SetPlaylistItemRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new SetPlaylistItemGroupRequest(requestData.PlaylistItemId);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request to remove items from the playlist in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The items to remove.</param>
        /// <response code="204">Queue update request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("RemoveFromPlaylist")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayRemoveFromPlaylist(
            [FromBody, Required] RemoveFromPlaylistRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new RemoveFromPlaylistGroupRequest(requestData.PlaylistItemIds);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request to move an item in the playlist in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The new position for the item.</param>
        /// <response code="204">Queue update request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("MovePlaylistItem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayMovePlaylistItem(
            [FromBody, Required] MovePlaylistItemRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new MovePlaylistItemGroupRequest(requestData.PlaylistItemId, requestData.NewIndex);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request to queue items to the playlist of a SyncPlay group.
        /// </summary>
        /// <param name="requestData">The items to add.</param>
        /// <response code="204">Queue update request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Queue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayQueue(
            [FromBody, Required] QueueRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new QueueGroupRequest(requestData.ItemIds, requestData.Mode);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request unpause in SyncPlay group.
        /// </summary>
        /// <response code="204">Unpause request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Unpause")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayUnpause()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new UnpauseGroupRequest();
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request pause in SyncPlay group.
        /// </summary>
        /// <response code="204">Pause request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Pause")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayPause()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PauseGroupRequest();
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request stop in SyncPlay group.
        /// </summary>
        /// <response code="204">Stop request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Stop")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayStop()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new StopGroupRequest();
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request seek in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The new playback position.</param>
        /// <response code="204">Seek request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Seek")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlaySeek(
            [FromBody, Required] SeekRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlayBuffering(
            [FromBody, Required] BufferRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlayReady(
            [FromBody, Required] ReadyRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlaySetIgnoreWait(
            [FromBody, Required] IgnoreWaitRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new IgnoreWaitGroupRequest(requestData.IgnoreWait);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request next track in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The current track information.</param>
        /// <response code="204">Next track request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("NextTrack")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayNextTrack(
            [FromBody, Required] NextTrackRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new NextTrackGroupRequest(requestData.PlaylistItemId);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request previous track in SyncPlay group.
        /// </summary>
        /// <param name="requestData">The current track information.</param>
        /// <response code="204">Previous track request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("PreviousTrack")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayPreviousTrack(
            [FromBody, Required] PreviousTrackRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PreviousTrackGroupRequest(requestData.PlaylistItemId);
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
        public ActionResult SyncPlaySetRepeatMode(
            [FromBody, Required] SetRepeatModeRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlaySetShuffleMode(
            [FromBody, Required] SetShuffleModeRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
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
        public ActionResult SyncPlayPing(
            [FromBody, Required] PingRequestBody requestData)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PingGroupRequest(requestData.Ping);
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }
    }
}
