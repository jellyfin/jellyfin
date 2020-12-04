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
        /// <param name="request">The generic request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupPlaybackRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a play request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The play request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(PlayGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-playlist-item request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The set-playlist-item request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SetPlaylistItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a remove-items request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The remove-items request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(RemoveFromPlaylistGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a move-playlist-item request from a session. Context's state should not change.
        /// </summary>
        /// <param name="request">The move-playlist-item request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(MovePlaylistItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a queue request from a session. Context's state should not change.
        /// </summary>
        /// <param name="request">The queue request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(QueueGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles an unpause request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The unpause request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(UnpauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a pause request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The pause request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(PauseGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a stop request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The stop request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(StopGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a seek request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The seek request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SeekGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffer request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The buffer request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(BufferGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a ready request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The ready request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(ReadyGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a next-item request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The next-item request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(NextItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a previous-item request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The previous-item request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(PreviousItemGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-repeat-mode request from a session. Context's state should not change.
        /// </summary>
        /// <param name="request">The repeat-mode request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SetRepeatModeGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a set-shuffle-mode request from a session. Context's state should not change.
        /// </summary>
        /// <param name="request">The shuffle-mode request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(SetShuffleModeGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the ping of a session. Context's state should not change.
        /// </summary>
        /// <param name="request">The ping request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(PingGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a ignore-wait request from a session. Context's state can change.
        /// </summary>
        /// <param name="request">The ignore-wait request.</param>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IgnoreWaitGroupRequest request, IGroupStateContext context, GroupStateType prevState, SessionInfo session, CancellationToken cancellationToken);
    }
}
