namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class NextItemRequestBody.
    /// </summary>
    public class NextItemRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextItemRequestBody"/> class.
        /// </summary>
        public NextItemRequestBody()
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
