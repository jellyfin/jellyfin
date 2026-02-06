#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayManager.
    /// </summary>
    public interface ISyncPlayManager
    {
        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="session">The session that's creating the group.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly created group.</returns>
        GroupInfoDto NewGroup(SessionInfo session, NewGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the session to a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void JoinGroup(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the session from a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void LeaveGroup(SessionInfo session, LeaveGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets list of available groups for a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <returns>The list of available groups.</returns>
        List<GroupInfoDto> ListGroups(SessionInfo session, ListGroupsRequest request);

        /// <summary>
        /// Gets available groups for a session by id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="groupId">The group id.</param>
        /// <returns>The groups or null.</returns>
        GroupInfoDto GetGroup(SessionInfo session, Guid groupId);

        /// <summary>
        /// Gets the authoritative V2 group state for the specified group.
        /// </summary>
        /// <param name="session">The current session.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>The V2 group state, or null when not available.</returns>
        SyncPlayGroupStateV2Dto GetGroupStateV2(SessionInfo session, Guid groupId);

        /// <summary>
        /// Gets the authoritative V2 group state for the current session's joined group.
        /// </summary>
        /// <param name="session">The current session.</param>
        /// <returns>The V2 group state, or null when session is not in a group.</returns>
        SyncPlayGroupStateV2Dto GetJoinedGroupStateV2(SessionInfo session);

        /// <summary>
        /// Handle a request by a session in a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SessionInfo session, IGroupPlaybackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a user has an active session using SyncPlay.
        /// </summary>
        /// <param name="userId">The user identifier to check.</param>
        /// <returns><c>true</c> if the user is using SyncPlay; <c>false</c> otherwise.</returns>
        bool IsUserActive(Guid userId);
    }
}
