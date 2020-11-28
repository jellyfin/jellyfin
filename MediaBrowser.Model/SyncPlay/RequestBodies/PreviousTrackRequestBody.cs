namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class PreviousTrackRequestBody.
    /// </summary>
    public class PreviousTrackRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousTrackRequestBody"/> class.
        /// </summary>
        public PreviousTrackRequestBody()
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
