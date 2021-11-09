using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Event details dto.
    /// </summary>
    public class EventDetailsDto
    {
        /// <summary>
        /// Gets or sets the sub type.
        /// </summary>
        [JsonPropertyName("subType")]
        public string? SubType { get; set; }
    }
}
