#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Channel dto.
    /// </summary>
    public class ChannelDto
    {
        /// <summary>
        /// Gets or sets the list of maps.
        /// </summary>
        [JsonPropertyName("map")]
        public List<MapDto> Map { get; set; }

        /// <summary>
        /// Gets or sets the list of stations.
        /// </summary>
        [JsonPropertyName("stations")]
        public List<StationDto> Stations { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        public MetadataDto Metadata { get; set; }
    }
}
