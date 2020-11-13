using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class RemoveFromPlaylistGroupRequest.
    /// </summary>
    public class RemoveFromPlaylistGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets the playlist identifiers ot the items.
        /// </summary>
        /// <value>The playlist identifiers ot the items.</value>
        public List<string> PlaylistItemIds { get; } = new List<string>();

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.RemoveFromPlaylist;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
