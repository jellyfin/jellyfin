namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The movie specific nfo tags.
    /// </summary>
    public class MovieNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        public string? SortTitle { get; set; }

        /// <summary>
        /// Gets or sets the IMDB Top 250 ranking.
        /// </summary>
        public int? Top250 { get; set; }

        /// <summary>
        /// Gets or sets the movie set.
        /// </summary>
        public SetNfo? Set { get; set; }

        /// <summary>
        /// Gets or sets the connected TV show.
        /// </summary>
        public string? ShowLink { get; set; }
    }
}
