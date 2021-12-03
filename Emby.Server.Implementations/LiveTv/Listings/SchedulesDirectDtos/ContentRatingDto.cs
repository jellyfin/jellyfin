using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Content rating dto.
    /// </summary>
    public class ContentRatingDto
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
