namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum UserDataSaveReason.
    /// </summary>
    public enum UserDataSaveReason
    {
        /// <summary>
        /// The playback start.
        /// </summary>
        PlaybackStart = 1,

        /// <summary>
        /// The playback progress.
        /// </summary>
        PlaybackProgress = 2,

        /// <summary>
        /// The playback finished.
        /// </summary>
        PlaybackFinished = 3,

        /// <summary>
        /// The toggle played.
        /// </summary>
        TogglePlayed = 4,

        /// <summary>
        /// The update user rating.
        /// </summary>
        UpdateUserRating = 5,

        /// <summary>
        /// The import.
        /// </summary>
        Import = 6
    }
}
