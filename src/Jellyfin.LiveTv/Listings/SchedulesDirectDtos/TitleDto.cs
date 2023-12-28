using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Title dto.
    /// </summary>
    public class TitleDto
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonPropertyName("title120")]
        public string? Title120 { get; set; }
    }
}
