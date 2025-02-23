namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing a subtitle playback mode.
    /// </summary>
    public enum SubtitlePlaybackMode
    {
        /// <summary>
        /// The default subtitle playback mode.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Always show subtitles.
        /// </summary>
        Always = 1,

        /// <summary>
        /// Only show forced subtitles.
        /// </summary>
        OnlyForced = 2,

        /// <summary>
        /// Don't show subtitles.
        /// </summary>
        None = 3,

        /// <summary>
        /// Only show subtitles when the current audio stream is in a different language.
        /// </summary>
        Smart = 4
    }
}
