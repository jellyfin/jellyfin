using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PingGroupRequest.
    /// </summary>
    public class PingGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the ping time.
        /// </summary>
        /// <value>The ping time.</value>
        public long Ping { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Ping;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
