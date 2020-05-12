using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SeekGroupRequest.
    /// </summary>
    public class SeekGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type()
        {
            return PlaybackRequestType.Seek;
        }

        /// <inheritdoc />
        public bool Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            return state.HandleRequest(context, false, this, session, cancellationToken);
        }
    }
}
