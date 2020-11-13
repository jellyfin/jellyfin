using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class NextTrackGroupRequest.
    /// </summary>
    public class NextTrackGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public string PlaylistItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.NextTrack;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
