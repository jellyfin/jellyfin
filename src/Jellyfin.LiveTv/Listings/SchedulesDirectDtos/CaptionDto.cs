using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Caption dto.
    /// </summary>
    public class CaptionDto
    {
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the lang.
        /// </summary>
        [JsonPropertyName("lang")]
        public string? Lang { get; set; }
    }
}
