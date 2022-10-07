#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class RemoveFromPlaylistGroupRequest.
    /// </summary>
    public class RemoveFromPlaylistGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveFromPlaylistGroupRequest"/> class.
        /// </summary>
        /// <param name="items">The playlist ids of the items to remove.</param>
        /// <param name="clearPlaylist">Whether to clear the entire playlist. The items list will be ignored.</param>
        /// <param name="clearPlayingItem">Whether to remove the playing item as well. Used only when clearing the playlist.</param>
        public RemoveFromPlaylistGroupRequest(IReadOnlyList<Guid> items, bool clearPlaylist = false, bool clearPlayingItem = false)
        {
            PlaylistItemIds = items ?? Array.Empty<Guid>();
            ClearPlaylist = clearPlaylist;
            ClearPlayingItem = clearPlayingItem;
        }

        /// <summary>
        /// Gets the playlist identifiers of the items.
        /// </summary>
        /// <value>The playlist identifiers of the items.</value>
        public IReadOnlyList<Guid> PlaylistItemIds { get; }

        /// <summary>
        /// Gets a value indicating whether the entire playlist should be cleared.
        /// </summary>
        /// <value>Whether the entire playlist should be cleared.</value>
        public bool ClearPlaylist { get; }

        /// <summary>
        /// Gets a value indicating whether the playing item should be removed as well.
        /// </summary>
        /// <value>Whether the playing item should be removed as well.</value>
        public bool ClearPlayingItem { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.RemoveFromPlaylist;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
