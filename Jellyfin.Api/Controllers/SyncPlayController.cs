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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("New")]
        public ActionResult CreateNewGroup()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            _syncPlayManager.NewGroup(currentSession, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Join an existing SyncPlay group.
        /// </summary>
        /// <param name="groupId">The sync play group id.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Join")]
        public ActionResult JoinGroup([FromQuery, Required] Guid groupId)
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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("Leave")]
        public ActionResult LeaveGroup()
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            _syncPlayManager.LeaveGroup(currentSession, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Gets all SyncPlay groups.
        /// </summary>
        /// <param name="filterItemId">Optional. Filter by item id.</param>
        /// <returns>An <see cref="IEnumerable{GrouüInfoView}"/> containing the available SyncPlay groups.</returns>
        [HttpGet("List")]
        public ActionResult<IEnumerable<GroupInfoView>> GetSyncPlayGroups([FromQuery] Guid? filterItemId)
        {
            var currentSession = RequestHelpers.GetSession(_sessionManager, _authorizationContext, Request);
            return Ok(_syncPlayManager.ListGroups(currentSession, filterItemId.HasValue ? filterItemId.Value : Guid.Empty));
        }

        /// <summary>
        /// Request play in SyncPlay group.
        /// </summary>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        public ActionResult Play()
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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        public ActionResult Pause()
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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        public ActionResult Seek([FromQuery] long positionTicks)
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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        public ActionResult Buffering([FromQuery] DateTime when, [FromQuery] long positionTicks, [FromQuery] bool bufferingDone)
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
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost]
        public ActionResult Ping([FromQuery] double ping)
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
