namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing the different options for the home screen sections.
    /// </summary>
    public enum HomeSectionType
    {
        /// <summary>
        /// My Media.
        /// </summary>
        SmallLibraryTiles = 0,

        /// <summary>
        /// My Media Small.
        /// </summary>
        LibraryButtons = 1,

        /// <summary>
        /// Active Recordings.
        /// </summary>
        ActiveRecordings = 2,

        /// <summary>
        /// Continue Watching.
        /// </summary>
        Resume = 3,

        /// <summary>
        /// Continue Listening.
        /// </summary>
        ResumeAudio = 4,

        /// <summary>
        /// Latest Media.
        /// </summary>
        LatestMedia = 5,

        /// <summary>
        /// Next Up.
        /// </summary>
        NextUp = 6,

        /// <summary>
        /// Live TV.
        /// </summary>
        LiveTv = 7,

        /// <summary>
        /// None.
        /// </summary>
        None = 8
    }
}
