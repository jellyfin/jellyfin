namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class MovePlaylistItemRequestBody.
    /// </summary>
    public class MovePlaylistItemRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovePlaylistItemRequestBody"/> class.
        /// </summary>
        public MovePlaylistItemRequestBody()
        {
            PlaylistItemId = string.Empty;
        }

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
    }
}
