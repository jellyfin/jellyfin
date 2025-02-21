namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing the different options for the home screen sections.
    /// </summary>
    public enum HomeSectionType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// My Media.
        /// </summary>
        SmallLibraryTiles = 1,

        /// <summary>
        /// My Media Small.
        /// </summary>
        LibraryButtons = 2,

        /// <summary>
        /// Active Recordings.
        /// </summary>
        ActiveRecordings = 3,

        /// <summary>
        /// Continue Watching.
        /// </summary>
        Resume = 4,

        /// <summary>
        /// Continue Listening.
        /// </summary>
        ResumeAudio = 5,

        /// <summary>
        /// Latest Media.
        /// </summary>
        LatestMedia = 6,

        /// <summary>
        /// Next Up.
        /// </summary>
        NextUp = 7,

        /// <summary>
        /// Live TV.
        /// </summary>
        LiveTv = 8,

        /// <summary>
        /// Continue Reading.
        /// </summary>
        ResumeBook = 9
    }
}
