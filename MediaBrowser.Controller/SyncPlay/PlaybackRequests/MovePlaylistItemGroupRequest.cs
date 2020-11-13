using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class MovePlaylistItemGroupRequest.
    /// </summary>
    public class MovePlaylistItemGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the playlist identifier of the item.
        /// </summary>
        /// <value>The playlist identifier of the item.</value>
        public string PlaylistItemId { get; set; }

        /// <summary>
        /// Gets or sets the new position.
        /// </summary>
        /// <value>The new position.</value>
        public int NewIndex { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.MovePlaylistItem;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
