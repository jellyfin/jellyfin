using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// The lineup dto.
    /// </summary>
    public class LineupDto
    {
        /// <summary>
        /// Gets or sets the lineup.
        /// </summary>
        [JsonPropertyName("lineup")]
        public string? Lineup { get; set; }

        /// <summary>
        /// Gets or sets the lineup name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the transport.
        /// </summary>
        [JsonPropertyName("transport")]
        public string? Transport { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this lineup was deleted.
        /// </summary>
        [JsonPropertyName("isDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
