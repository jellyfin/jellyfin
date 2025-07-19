using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Metadata dto.
    /// </summary>
    public class MetadataDto
    {
        /// <summary>
        /// Gets or sets the lineup.
        /// </summary>
        [JsonPropertyName("lineup")]
        public string? Lineup { get; set; }

        /// <summary>
        /// Gets or sets the modified timestamp.
        /// </summary>
        [JsonPropertyName("modified")]
        public string? Modified { get; set; }

        /// <summary>
        /// Gets or sets the transport.
        /// </summary>
        [JsonPropertyName("transport")]
        public string? Transport { get; set; }
    }
}
