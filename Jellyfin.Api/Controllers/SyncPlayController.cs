using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Model.SyncPlay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The sync play controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
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
        /// <response code="204">New group created.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("New")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayCreateGroup()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            _syncPlayManager.NewGroup(currentSession, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Join an existing SyncPlay group.
        /// </summary>
        /// <param name="groupId">The sync play group id.</param>
        /// <response code="204">Group join successful.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Join")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayJoinGroup([FromQuery, Required] Guid groupId)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);

            var joinRequest = new JoinGroupRequest()
            {
                GroupId = groupId
            };

            _syncPlayManager.JoinGroup(currentSession, groupId, joinRequest, CancellationToken.None);
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
            _syncPlayManager.LeaveGroup(currentSession, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Gets all SyncPlay groups.
        /// </summary>
        /// <param name="filterItemId">Optional. Filter by item id.</param>
        /// <response code="200">Groups returned.</response>
        /// <returns>An <see cref="IEnumerable{GroupInfoView}"/> containing the available SyncPlay groups.</returns>
        [HttpGet("List")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<GroupInfoView>> SyncPlayGetGroups([FromQuery] Guid? filterItemId)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            return Ok(_syncPlayManager.ListGroups(currentSession, filterItemId.HasValue ? filterItemId.Value : Guid.Empty));
        }

        /// <summary>
        /// Request play in SyncPlay group.
        /// </summary>
        /// <response code="204">Play request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Play")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayPlay()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Play
            };
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
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Pause
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request seek in SyncPlay group.
        /// </summary>
        /// <param name="positionTicks">The playback position in ticks.</param>
        /// <response code="204">Seek request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Seek")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlaySeek([FromQuery] long positionTicks)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Seek,
                PositionTicks = positionTicks
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Request group wait in SyncPlay group while buffering.
        /// </summary>
        /// <param name="when">When the request has been made by the client.</param>
        /// <param name="positionTicks">The playback position in ticks.</param>
        /// <param name="bufferingDone">Whether the buffering is done.</param>
        /// <response code="204">Buffering request sent to all group members.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Buffering")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayBuffering([FromQuery] DateTime when, [FromQuery] long positionTicks, [FromQuery] bool bufferingDone)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = bufferingDone ? PlaybackRequestType.Ready : PlaybackRequestType.Buffer,
                When = when,
                PositionTicks = positionTicks
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Update session ping.
        /// </summary>
        /// <param name="ping">The ping.</param>
        /// <response code="204">Ping updated.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Ping")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SyncPlayPing([FromQuery] double ping)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            var syncPlayRequest = new PlaybackRequest()
            {
                Type = PlaybackRequestType.Ping,
                Ping = Convert.ToInt64(ping)
            };
            _syncPlayManager.HandleRequest(currentSession, syncPlayRequest, CancellationToken.None);
            return NoContent();
        }
    }
}
