using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class UnpauseGroupRequest.
    /// </summary>
    public class UnpauseGroupRequest : IGroupPlaybackRequest
    {
        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Unpause;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
