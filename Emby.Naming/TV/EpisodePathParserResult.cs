namespace Emby.Naming.TV
{
    /// <summary>
    /// Holder object for <see cref="EpisodePathParser"/> result.
    /// </summary>
    public class EpisodePathParserResult
    {
        /// <summary>
        /// Gets or sets optional season number.
        /// </summary>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets optional episode number.
        /// </summary>
        public int? EpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets optional ending episode number. For multi-episode files 1-13.
        /// </summary>
        public int? EndingEpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string? SeriesName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether parsing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether by date expression was used.
        /// </summary>
        public bool IsByDate { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// Gets or sets optional day of release.
        /// </summary>
        public int? Day { get; set; }
    }
}
