using System.Threading;
using MediaBrowser.Controller.Session;
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
        /// Generic handle. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The generic action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, IGroupPlaybackRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a play action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The play action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a playlist-item change requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The playlist-item change action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetPlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a remove-items change requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The remove-items change action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, RemoveFromPlaylistGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a move-item change requested by a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The move-item change action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, MovePlaylistItemGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a queue change requested by a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The queue action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, QueueGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles an unpause action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The unpause action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, UnpauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a pause action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The pause action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a stop action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The stop action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, StopGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a seek action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The seek action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffering action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The buffering action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffering-done action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The buffering-done action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a next-track action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The next-track action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, NextTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a previous-track action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The previous-track action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PreviousTrackGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a repeat-mode change requested by a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The repeat-mode action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetRepeatModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a shuffle-mode change requested by a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The shuffle-mode action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, SetShuffleModeGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Updates ping of a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The buffering-done action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Updates whether the session should be considered during group wait. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="prevState">The previous state.</param>
        /// <param name="request">The ignore-wait action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void HandleRequest(IGroupStateContext context, GroupStateType prevState, IgnoreWaitGroupRequest request, SessionInfo session, CancellationToken cancellationToken);
    }
}
