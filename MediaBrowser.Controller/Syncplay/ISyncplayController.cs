using System;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Syncplay;

namespace MediaBrowser.Controller.Syncplay
{
    /// <summary>
    /// Interface ISyncplayController.
    /// </summary>
    public interface ISyncplayController
    {
        /// <summary>
        /// Gets the group id.
        /// </summary>
        /// <value>The group id.</value>
        Guid GetGroupId();

        /// <summary>
        /// Gets the playing item id.
        /// </summary>
        /// <value>The playing item id.</value>
        Guid GetPlayingItemId();

        /// <summary>
        /// Checks if the group is empty.
        /// </summary>
        /// <value>If the group is empty.</value>
        bool IsGroupEmpty();

        /// <summary>
        /// Initializes the group with the session's info.
        /// </summary>
        /// <param name="session">The session.</param>
        void InitGroup(SessionInfo session);

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        void SessionJoin(SessionInfo session);

        /// <summary>
        /// Removes the session from the group.
        /// </summary>
        /// <param name="session">The session.</param>
        void SessionLeave(SessionInfo session);

        /// <summary>
        /// Handles the requested action by the session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The requested action.</param>
        void HandleRequest(SessionInfo session, SyncplayRequestInfo request);

        /// <summary>
        /// Gets the info about the group for the clients.
        /// </summary>
        /// <value>The group info for the clients.</value>
        GroupInfoView GetInfo();
    }
}