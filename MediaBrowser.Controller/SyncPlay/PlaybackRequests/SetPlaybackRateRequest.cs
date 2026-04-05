#nullable disable

using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class SetPlaybackRateRequest.
    /// </summary>
    public class SetPlaybackRateRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetPlaybackRateRequest"/> class.
        /// </summary>
        /// <param name="playbackRate">The playback rate.</param>
        public SetPlaybackRateRequest(float playbackRate)
        {
            PlaybackRate = playbackRate;
        }

        /// <summary>
        /// Gets the playback rate.
        /// </summary>
        /// <value>The playback rate.</value>
        public float PlaybackRate { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.SetPlaybackRate;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
