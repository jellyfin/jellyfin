#nullable disable

using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Map dto.
    /// </summary>
    public class MapDto
    {
        /// <summary>
        /// Gets or sets the station id.
        /// </summary>
        [JsonPropertyName("stationID")]
        public string StationId { get; set; }

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the logical channel number.
        /// </summary>
        [JsonPropertyName("logicalChannelNumber")]
        public string LogicalChannelNumber { get; set; }

        /// <summary>
        /// Gets or sets the uhfvhf.
        /// </summary>
        [JsonPropertyName("uhfVhf")]
        public int UhfVhf { get; set; }

        /// <summary>
        /// Gets or sets the astc major.
        /// </summary>
        [JsonPropertyName("astcMajor")]
        public int AtscMajor { get; set; }

        /// <summary>
        /// Gets or sets the astc minor.
        /// </summary>
        [JsonPropertyName("astcMinor")]
        public int AtscMinor { get; set; }
    }
}
