namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The tv series specific nfo tags.
    /// </summary>
    public class SeriesNfo
    {
        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        public string? SortTitle { get; set; }

        /// <summary>
        /// Gets or sets the show title / alternative title.
        /// </summary>
        public string? ShowTitle { get; set; }

        /// <summary>
        /// Gets or sets the IMDB Top 250 ranking.
        /// </summary>
        public int? Top250 { get; set; }

        /// <summary>
        /// Gets or sets the number of seasons.
        /// </summary>
        public int? Season { get; set; }

        /// <summary>
        /// Gets or sets the number of episodes.
        /// </summary>
        public int? Episode { get; set; }

        // TODO Displayepisode

        // TODO Displayseason

        // TODO namedseason
    }
}
