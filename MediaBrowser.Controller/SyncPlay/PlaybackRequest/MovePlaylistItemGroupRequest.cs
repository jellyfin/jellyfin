using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class MovePlaylistItemGroupRequest.
    /// </summary>
    public class MovePlaylistItemGroupRequest : IPlaybackGroupRequest
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
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.MovePlaylistItem;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
