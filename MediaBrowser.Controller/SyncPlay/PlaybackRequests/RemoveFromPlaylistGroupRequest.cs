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
        public RemoveFromPlaylistGroupRequest(IReadOnlyList<Guid> items)
        {
            PlaylistItemIds = items ?? Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets the playlist identifiers ot the items.
        /// </summary>
        /// <value>The playlist identifiers ot the items.</value>
        public IReadOnlyList<Guid> PlaylistItemIds { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.RemoveFromPlaylist;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
