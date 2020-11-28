namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class SetShuffleModeRequestBody.
    /// </summary>
    public class SetShuffleModeRequestBody
    {
        /// <summary>
        /// Gets or sets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode Mode { get; set; }
    }
}
