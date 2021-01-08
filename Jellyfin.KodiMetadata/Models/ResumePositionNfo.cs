namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The nfo resumeposition tag.
    /// </summary>
    public class ResumePositionNfo
    {
        /// <summary>
        /// Gets or sets the resume point in seconds.
        /// </summary>
        public float? Position { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        public float? Total { get; set; }
    }
}
