namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// An enum representing the options to disable embedded subs.
    /// </summary>
    public enum EmbeddedSubtitleOptions
    {
        /// <summary>
        /// Allow all embedded subs.
        /// </summary>
        AllowAll = 0,

        /// <summary>
        /// Allow only embedded subs that are text based.
        /// </summary>
        AllowText = 1,

        /// <summary>
        /// Allow only embedded subs that are image based.
        /// </summary>
        AllowImage = 2,

        /// <summary>
        /// Disable all embedded subs.
        /// </summary>
        AllowNone = 3,
    }
}
