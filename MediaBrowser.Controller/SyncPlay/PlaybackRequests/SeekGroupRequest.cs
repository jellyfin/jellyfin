#nullable disable

using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class SeekGroupRequest.
    /// </summary>
    public class SeekGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeekGroupRequest"/> class.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        public SeekGroupRequest(long positionTicks)
        {
            PositionTicks = positionTicks;
        }

        /// <summary>
        /// Gets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.Seek;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
