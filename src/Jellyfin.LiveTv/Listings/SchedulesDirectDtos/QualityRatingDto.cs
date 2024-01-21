using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Quality rating dto.
    /// </summary>
    public class QualityRatingDto
    {
        /// <summary>
        /// Gets or sets the ratings body.
        /// </summary>
        [JsonPropertyName("ratingsBody")]
        public string? RatingsBody { get; set; }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        [JsonPropertyName("rating")]
        public string? Rating { get; set; }

        /// <summary>
        /// Gets or sets the min rating.
        /// </summary>
        [JsonPropertyName("minRating")]
        public string? MinRating { get; set; }

        /// <summary>
        /// Gets or sets the max rating.
        /// </summary>
        [JsonPropertyName("maxRating")]
        public string? MaxRating { get; set; }

        /// <summary>
        /// Gets or sets the increment.
        /// </summary>
        [JsonPropertyName("increment")]
        public string? Increment { get; set; }
    }
}
