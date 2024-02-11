using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
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
        public IReadOnlyList<MapDto> Map { get; set; } = Array.Empty<MapDto>();

        /// <summary>
        /// Gets or sets the list of stations.
        /// </summary>
        [JsonPropertyName("stations")]
        public IReadOnlyList<StationDto> Stations { get; set; } = Array.Empty<StationDto>();

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        public MetadataDto? Metadata { get; set; }
    }
}
