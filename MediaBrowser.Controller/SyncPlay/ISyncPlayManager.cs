using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
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
        /// <param name="cancellationToken">The cancellation token.</param>
        void NewGroup(SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the session to a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void JoinGroup(SessionInfo session, Guid groupId, JoinGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the session from a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void LeaveGroup(SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Gets list of available groups for a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="filterItemId">The item id to filter by.</param>
        /// <value>The list of available groups.</value>
        List<GroupInfoView> ListGroups(SessionInfo session, Guid filterItemId);

        /// <summary>
        /// Handle a request by a session in a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Maps a session to a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void AddSessionToGroup(SessionInfo session, ISyncPlayController group);

        /// <summary>
        /// Unmaps a session from a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void RemoveSessionFromGroup(SessionInfo session, ISyncPlayController group);
    }
}
