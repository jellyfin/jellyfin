using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface ISyncPlayState.
    /// </summary>
    public interface ISyncPlayState
    {
        /// <summary>
        /// Gets the group state.
        /// </summary>
        /// <value>The group state.</value>
        GroupState GetGroupState();

        /// <summary>
        /// Generic handle. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The play action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, IPlaybackGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a play action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The play action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a pause action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The pause action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a seek action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The seek action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffering action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The buffering action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Handles a buffering-done action requested by a session. Context's state can change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The buffering-done action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken);

        /// <summary>
        /// Updates ping of a session. Context's state should not change.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="newState">Whether the state has been just set.</param>
        /// <param name="request">The buffering-done action.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <value>The operation completion status.</value>
        bool HandleRequest(ISyncPlayStateContext context, bool newState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken);
    }
}
