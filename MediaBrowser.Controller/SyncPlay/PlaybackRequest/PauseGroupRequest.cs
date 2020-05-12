using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PauseGroupRequest.
    /// </summary>
    public class PauseGroupRequest : IPlaybackGroupRequest
    {
        /// <inheritdoc />
        public PlaybackRequestType Type()
        {
            return PlaybackRequestType.Pause;
        }

        /// <inheritdoc />
        public bool Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            return state.HandleRequest(context, false, this, session, cancellationToken);
        }
    }
}
