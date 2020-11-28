using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class NextTrackGroupRequest.
    /// </summary>
    public class NextTrackGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextTrackGroupRequest"/> class.
        /// </summary>
        /// <param name="playlistItemId">The playing item identifier.</param>
        public NextTrackGroupRequest(string playlistItemId)
        {
            PlaylistItemId = playlistItemId;
        }

        /// <summary>
        /// Gets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public string PlaylistItemId { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.NextTrack;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
