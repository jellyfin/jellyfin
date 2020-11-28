using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class SetPlaylistItemGroupRequest.
    /// </summary>
    public class SetPlaylistItemGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetPlaylistItemGroupRequest"/> class.
        /// </summary>
        /// <param name="playlistItemId">The playlist identifier of the item.</param>
        public SetPlaylistItemGroupRequest(string playlistItemId)
        {
            PlaylistItemId = playlistItemId;
        }

        /// <summary>
        /// Gets the playlist identifier of the playing item.
        /// </summary>
        /// <value>The playlist identifier of the playing item.</value>
        public string PlaylistItemId { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.SetPlaylistItem;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
