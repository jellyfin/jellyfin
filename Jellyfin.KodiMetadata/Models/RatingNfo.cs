namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The nfo rating tag.
    /// </summary>
    public class RatingNfo
    {
        /// <summary>
        /// Gets or sets the name of the rating.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the maximum rating value.
        /// </summary>
        public int? Max { get; set; }

        /// <summary>
        /// Gets or sets the actual rating value.
        /// </summary>
        public float? Value { get; set; }

        /// <summary>
        /// Gets or sets the number of votes.
        /// </summary>
        public int? Votes { get; set; }
    }
}
