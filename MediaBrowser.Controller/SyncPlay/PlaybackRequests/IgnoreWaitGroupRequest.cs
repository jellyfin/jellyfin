using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class IgnoreWaitGroupRequest.
    /// </summary>
    public class IgnoreWaitGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client should be ignored.
        /// </summary>
        /// <value>The client group-wait status.</value>
        public bool IgnoreWait { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.IgnoreWait;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
