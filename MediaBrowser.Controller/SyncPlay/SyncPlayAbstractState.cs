using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SyncPlayAbstractState.
    /// </summary>
    /// <remarks>
    /// Class is not thread-safe, external locking is required when accessing methods.
    /// </remarks>
    public abstract class SyncPlayAbstractState : ISyncPlayState
    {
        /// <inheritdoc />
        public abstract GroupState GetGroupState();

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, IPlaybackGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, PlayGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, PauseGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, SeekGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, BufferGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, ReadyGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool HandleRequest(ISyncPlayStateContext context, bool newState, PingGroupRequest request, SessionInfo session, CancellationToken cancellationToken)
        {
            GroupInfo group = context.GetGroup();

            // Collected pings are used to account for network latency when unpausing playback
            group.UpdatePing(session, request.Ping);

            return true;
        }
    }
}
