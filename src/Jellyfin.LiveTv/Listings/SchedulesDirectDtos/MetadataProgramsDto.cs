using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Metadata programs dto.
    /// </summary>
    public class MetadataProgramsDto
    {
        /// <summary>
        /// Gets or sets the gracenote object.
        /// </summary>
        [JsonPropertyName("Gracenote")]
        public GracenoteDto? Gracenote { get; set; }
    }
}
