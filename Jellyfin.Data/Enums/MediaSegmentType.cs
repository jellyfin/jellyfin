namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// Enum MediaSegmentType.
    /// </summary>
    public enum MediaSegmentType
    {
        /// <summary>
        /// The Intro.
        /// </summary>
        Intro = 0,

        /// <summary>
        /// The Outro.
        /// </summary>
        Outro = 1,

        /// <summary>
        /// Recap of last tv show episode(s).
        /// </summary>
        Recap = 2,

        /// <summary>
        /// The preview for the next tv show episode.
        /// </summary>
        Preview = 3,

        /// <summary>
        /// Commercial that interrupt the viewer.
        /// </summary>
        Commercial = 4,
    }
}
