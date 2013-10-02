
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum UserDataSaveReason
    /// </summary>
    public enum UserDataSaveReason
    {
        /// <summary>
        /// The playback start
        /// </summary>
        PlaybackStart,
        /// <summary>
        /// The playback progress
        /// </summary>
        PlaybackProgress,
        /// <summary>
        /// The playback finished
        /// </summary>
        PlaybackFinished,
        /// <summary>
        /// The toggle played
        /// </summary>
        TogglePlayed,
        /// <summary>
        /// The update user rating
        /// </summary>
        UpdateUserRating
    }
}
