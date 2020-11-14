using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class PingGroupRequest.
    /// </summary>
    public class PingGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PingGroupRequest"/> class.
        /// </summary>
        /// <param name="ping">The ping time.</param>
        public PingGroupRequest(long ping)
        {
            Ping = ping;
        }

        /// <summary>
        /// Gets the ping time.
        /// </summary>
        /// <value>The ping time.</value>
        public long Ping { get; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Ping;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
