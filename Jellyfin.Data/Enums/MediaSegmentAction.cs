namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// Enum MediaSegmentAction.
    /// </summary>
    public enum MediaSegmentAction
    {
        /// <summary>
        /// Auto, use default for type.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// None, do nothing with MediaSegment.
        /// </summary>
        None = 1,

        /// <summary>
        /// Force skip the MediaSegment.
        /// </summary>
        Skip = 2,

        /// <summary>
        /// Prompt user to skip the MediaSegment.
        /// </summary>
        Prompt = 3,

        /// <summary>
        /// Mute the MediaSegment.
        /// </summary>
        Mute = 4,
    }
}
