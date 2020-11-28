namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class NextTrackRequestBody.
    /// </summary>
    public class NextTrackRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextTrackRequestBody"/> class.
        /// </summary>
        public NextTrackRequestBody()
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
