namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class SetPlaylistItemRequestBody.
    /// </summary>
    public class SetPlaylistItemRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetPlaylistItemRequestBody"/> class.
        /// </summary>
        public SetPlaylistItemRequestBody()
        {
            PlaylistItemId = string.Empty;
        }

        /// <summary>
        /// Gets or sets the playlist identifier of the playing item.
        /// </summary>
        /// <value>The playlist identifier of the playing item.</value>
        public string PlaylistItemId { get; set; }
    }
}
