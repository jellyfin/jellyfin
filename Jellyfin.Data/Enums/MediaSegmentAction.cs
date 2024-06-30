namespace Jellyfin.Data.Enums.MediaSegmentAction
{
    /// <summary>
    /// An enum representing the Action of MediaSegment.
    /// </summary>
    public enum MediaSegmentAction
    {
        /// <summary>
        /// None, do nothing with MediaSegment.
        /// </summary>
        None = 0,

        /// <summary>
        /// Force skip the MediaSegment.
        /// </summary>
        Skip = 1,

        /// <summary>
        /// Prompt user to skip the MediaSegment.
        /// </summary>
        PromptToSkip = 2,

        /// <summary>
        /// Mute the MediaSegment.
        /// </summary>
        Mute = 3,
    }
}
