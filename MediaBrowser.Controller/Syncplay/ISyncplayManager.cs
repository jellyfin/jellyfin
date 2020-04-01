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
        /// <param name="user">The user that's creating the group.</param>
        void NewGroup(SessionInfo user);

        /// <summary>
        /// Adds the user to a group.
        /// </summary>
        /// <param name="user">The session.</param>
        /// <param name="groupId">The group id.</param>
        void JoinGroup(SessionInfo user, string groupId);

        /// <summary>
        /// Removes the user from a group.
        /// </summary>
        /// <param name="user">The session.</param>
        void LeaveGroup(SessionInfo user);

        /// <summary>
        /// Gets list of available groups for a user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <value>The list of available groups.</value>
        List<GroupInfoView> ListGroups(SessionInfo user);

        /// <summary>
        /// Handle a request by a user in a group.
        /// </summary>
        /// <param name="user">The session.</param>
        /// <param name="request">The request.</param>
        void HandleRequest(SessionInfo user, SyncplayRequestInfo request);

        /// <summary>
        /// Maps a user to a group.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void MapUserToGroup(SessionInfo user, ISyncplayController group);

        /// <summary>
        /// Unmaps a user from a group.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="group">The group.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void UnmapUserFromGroup(SessionInfo user, ISyncplayController group);
    }
}
