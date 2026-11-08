namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// The type of a trailer.
    /// </summary>
    public enum TrailerType
    {
        /// <summary>
        /// A trailer for media coming soon to theaters.
        /// </summary>
        ComingSoonToTheaters = 1,

        /// <summary>
        /// A trailer for media coming soon to DVD.
        /// </summary>
        ComingSoonToDvd = 2,

        /// <summary>
        /// A trailer for media coming soon to streaming services.
        /// </summary>
        ComingSoonToStreaming = 3,

        /// <summary>
        /// An archived trailer.
        /// </summary>
        Archive = 4,

        /// <summary>
        /// A local trailer stored alongside the media.
        /// </summary>
        LocalTrailer = 5
    }
}
