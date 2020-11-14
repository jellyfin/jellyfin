using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface IGroupState.
    /// </summary>
    public interface IGroupState
    {
        /// <summary>
        /// Gets the group state type.
        /// </summary>
        /// <value>The group state type.</value>
        GroupStateType Type { get; }

        /// <summary>
        /// Handles a session that joined the group.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionJoined(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a session that is leaving the group.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SessionLeaving(IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Generic handler. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The generic request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, IGroupPlaybackRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a play request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The play request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-playlist-item request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The set-playlist-item request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a remove-items request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The remove-items request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, RemoveFromPlaylistGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a move-playlist-item request from a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The move-playlist-item request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, MovePlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a queue request from a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The queue request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, QueueGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles an unpause request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The unpause request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a pause request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The pause request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a stop request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The stop request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a seek request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The seek request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffer request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The buffer request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a ready request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The ready request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a next-track request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The next-track request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a previous-track request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The previous-track request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-repeat-mode request from a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The repeat-mode request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetRepeatModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-shuffle-mode request from a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The shuffle-mode request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetShuffleModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the ping of a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The ping request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a ignore-wait request from a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The ignore-wait request.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, IgnoreWaitGroupRequest request, SessionInfo session, CancellationToken cancellationToken);
    }
}
