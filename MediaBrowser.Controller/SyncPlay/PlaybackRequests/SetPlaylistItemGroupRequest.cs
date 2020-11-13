using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SetPlaylistItemGroupRequest.
    /// </summary>
    public class SetPlaylistItemGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the playlist identifier of the playing item.
        /// </summary>
        /// <value>The playlist identifier of the playing item.</value>
        public string PlaylistItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.SetPlaylistItem;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
