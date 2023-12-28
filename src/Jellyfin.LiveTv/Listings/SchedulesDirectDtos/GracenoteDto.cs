using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Gracenote dto.
    /// </summary>
    public class GracenoteDto
    {
        /// <summary>
        /// Gets or sets the season.
        /// </summary>
        [JsonPropertyName("season")]
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the episode.
        /// </summary>
        [JsonPropertyName("episode")]
        public int Episode { get; set; }
    }
}
