using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class PreviousTrackGroupRequest.
    /// </summary>
    public class PreviousTrackGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousTrackGroupRequest"/> class.
        /// </summary>
        /// <param name="playlistItemId">The playing item identifier.</param>
        public PreviousTrackGroupRequest(string playlistItemId)
        {
            PlaylistItemId = playlistItemId;
        }

        /// <summary>
        /// Gets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public string PlaylistItemId { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.PreviousTrack;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
