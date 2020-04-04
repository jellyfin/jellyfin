using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Syncplay;

namespace MediaBrowser.Controller.Syncplay
{
    /// <summary>
    /// Interface ISyncplayManager.
    /// </summary>
    public interface ISyncplayManager
    {
        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="session">The session that's creating the group.</param>
        void NewGroup(SessionInfo session);

        /// <summary>
        /// Adds the session to a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="groupId">The group id.</param>
        void JoinGroup(SessionInfo session, string groupId);

        /// <summary>
        /// Removes the session from a group.
        /// </summary>
        /// <param name="session">The session.</param>
        void LeaveGroup(SessionInfo session);

        /// <summary>
        /// Gets list of available groups for a session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <value>The list of available groups.</value>
        List<GroupInfoView> ListGroups(SessionInfo session);

        /// <summary>
        /// Handle a request by a session in a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        void HandleRequest(SessionInfo session, SyncplayRequestInfo request);

        /// <summary>
        /// Maps a session to a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void MapSessionToGroup(SessionInfo session, ISyncplayController group);

        /// <summary>
        /// Unmaps a session from a group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void UnmapSessionFromGroup(SessionInfo session, ISyncplayController group);
    }
}
