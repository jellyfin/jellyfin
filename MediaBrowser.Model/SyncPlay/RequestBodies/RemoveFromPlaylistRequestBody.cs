using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class RemoveFromPlaylistRequestBody.
    /// </summary>
    public class RemoveFromPlaylistRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveFromPlaylistRequestBody"/> class.
        /// </summary>
        public RemoveFromPlaylistRequestBody()
        {
            PlaylistItemIds = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the playlist identifiers ot the items.
        /// </summary>
        /// <value>The playlist identifiers ot the items.</value>
        public IReadOnlyList<string> PlaylistItemIds { get; set; }
    }
}
