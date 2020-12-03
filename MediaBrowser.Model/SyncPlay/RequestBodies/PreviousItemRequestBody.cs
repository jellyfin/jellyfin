namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class PreviousItemRequestBody.
    /// </summary>
    public class PreviousItemRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousItemRequestBody"/> class.
        /// </summary>
        public PreviousItemRequestBody()
        {
            PlaylistItemId = string.Empty;
        }

        /// <summary>
        /// Gets or sets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public string PlaylistItemId { get; set; }
    }
}
