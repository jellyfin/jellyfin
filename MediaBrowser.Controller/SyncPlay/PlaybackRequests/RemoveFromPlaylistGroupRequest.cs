using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class RemoveFromPlaylistGroupRequest.
    /// </summary>
    public class RemoveFromPlaylistGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveFromPlaylistGroupRequest"/> class.
        /// </summary>
        /// <param name="items">The playlist ids of the items to remove.</param>
        public RemoveFromPlaylistGroupRequest(string[] items)
        {
            var list = new List<string>();
            list.AddRange(items);
            PlaylistItemIds = list;
        }

        /// <summary>
        /// Gets the playlist identifiers ot the items.
        /// </summary>
        /// <value>The playlist identifiers ot the items.</value>
        public IReadOnlyList<string> PlaylistItemIds { get; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.RemoveFromPlaylist;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
