using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PauseGroupRequest.
    /// </summary>
    public class PauseGroupRequest : IGroupPlaybackRequest
    {
        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Pause;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
