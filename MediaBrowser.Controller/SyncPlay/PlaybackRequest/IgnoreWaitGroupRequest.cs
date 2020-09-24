using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class IgnoreWaitGroupRequest.
    /// </summary>
    public class IgnoreWaitGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the client group-wait status.
        /// </summary>
        /// <value>The client group-wait status.</value>
        public bool IgnoreWait { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.IgnoreWait;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
