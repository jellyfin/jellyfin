using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
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
        public string? StationId { get; set; }

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the provider callsign.
        /// </summary>
        [JsonPropertyName("providerCallsign")]
        public string? ProviderCallsign { get; set; }

        /// <summary>
        /// Gets or sets the logical channel number.
        /// </summary>
        [JsonPropertyName("logicalChannelNumber")]
        public string? LogicalChannelNumber { get; set; }

        /// <summary>
        /// Gets or sets the uhfvhf.
        /// </summary>
        [JsonPropertyName("uhfVhf")]
        public int UhfVhf { get; set; }

        /// <summary>
        /// Gets or sets the atsc major.
        /// </summary>
        [JsonPropertyName("atscMajor")]
        public int AtscMajor { get; set; }

        /// <summary>
        /// Gets or sets the atsc minor.
        /// </summary>
        [JsonPropertyName("atscMinor")]
        public int AtscMinor { get; set; }

        /// <summary>
        /// Gets or sets the match type.
        /// </summary>
        [JsonPropertyName("matchType")]
        public string? MatchType { get; set; }
    }
}
