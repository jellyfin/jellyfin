using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayController.
    /// </summary>
    public interface ISyncPlayController
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
        /// <param name="cancellationToken">The cancellation token.</param>
        void InitGroup(SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the session from the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionLeave(SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles the requested action by the session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The requested action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SessionInfo session, PlaybackRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the info about the group for the clients.
        /// </summary>
        /// <value>The group info for the clients.</value>
        GroupInfoView GetInfo();
    }
}