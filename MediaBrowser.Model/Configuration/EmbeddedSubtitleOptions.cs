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
        AllowAll,

        /// <summary>
        /// Allow only embedded subs that are text based.
        /// </summary>
        AllowText,

        /// <summary>
        /// Allow only embedded subs that are image based.
        /// </summary>
        AllowImage,

        /// <summary>
        /// Disable all embedded subs.
        /// </summary>
        AllowNone,
    }

}
