using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

// FIXME: not really group related, can be moved up to SyncPlayController maybe?
namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class UpdatePingGroupRequest.
    /// </summary>
    public class PingGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the ping time.
        /// </summary>
        /// <value>The ping time.</value>
        public long Ping { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type()
        {
            return PlaybackRequestType.Ping;
        }

        /// <inheritdoc />
        public bool Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            return state.HandleRequest(context, false, this, session, cancellationToken);
        }
    }
}
