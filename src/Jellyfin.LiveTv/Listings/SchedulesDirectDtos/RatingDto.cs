using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Rating dto.
    /// </summary>
    public class RatingDto
    {
        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
