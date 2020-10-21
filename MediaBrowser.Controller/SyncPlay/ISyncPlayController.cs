using System;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayGroupController.
    /// </summary>
    public interface ISyncPlayGroupController
    {
        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        Guid GroupId { get; }

        /// <summary>
        /// Gets the play queue.
        /// </summary>
        /// <value>The play queue.</value>
        PlayQueueManager PlayQueue { get; }

        /// <summary>
        /// Checks if the group is empty.
        /// </summary>
        /// <returns><c>true</c> if the group is empty, <c>false</c> otherwise</returns>
        bool IsGroupEmpty();

        /// <summary>
        /// Initializes the group with the session's info.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void CreateGroup(SessionInfo session, NewGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the session to the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionJoin(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Restores the state of a session that already joined the group.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionRestore(SessionInfo session, JoinGroupRequest request, CancellationToken cancellationToken);

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
        void HandleRequest(SessionInfo session, IPlaybackGroupRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the info about the group for the clients.
        /// </summary>
        /// <returns>The group info for the clients.</returns>
        GroupInfoDto GetInfo();

        /// <summary>
        /// Checks if a user has access to all content in the play queue.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user can access the play queue; <c>false</c> otherwise.</returns>
        bool HasAccessToPlayQueue(User user);

    }
}
