#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public enum SubtitlePlaybackMode
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,

        /// <summary>
        /// Always
        /// </summary>
        Always = 1,

        /// <summary>
        /// Only forced
        /// </summary>
        OnlyForced = 2,

        /// <summary>
        /// None
        /// </summary>
        None = 3,

        /// <summary>
        /// Smart
        /// </summary>
        Smart = 4
    }
}
