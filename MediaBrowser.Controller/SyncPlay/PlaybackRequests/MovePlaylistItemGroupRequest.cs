#nullable disable

using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class MovePlaylistItemGroupRequest.
    /// </summary>
    public class MovePlaylistItemGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovePlaylistItemGroupRequest"/> class.
        /// </summary>
        /// <param name="playlistItemId">The playlist identifier of the item.</param>
        /// <param name="newIndex">The new position.</param>
        public MovePlaylistItemGroupRequest(Guid playlistItemId, int newIndex)
        {
            PlaylistItemId = playlistItemId;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Gets the playlist identifier of the item.
        /// </summary>
        /// <value>The playlist identifier of the item.</value>
        public Guid PlaylistItemId { get; }

        /// <summary>
        /// Gets the new position.
        /// </summary>
        /// <value>The new position.</value>
        public int NewIndex { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.MovePlaylistItem;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
