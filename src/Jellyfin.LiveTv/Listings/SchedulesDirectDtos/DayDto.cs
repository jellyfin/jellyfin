using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Day dto.
    /// </summary>
    public class DayDto
    {
        /// <summary>
        /// Gets or sets the station id.
        /// </summary>
        [JsonPropertyName("stationID")]
        public string? StationId { get; set; }

        /// <summary>
        /// Gets or sets the list of programs.
        /// </summary>
        [JsonPropertyName("programs")]
        public IReadOnlyList<ProgramDto> Programs { get; set; } = Array.Empty<ProgramDto>();

        /// <summary>
        /// Gets or sets the metadata schedule.
        /// </summary>
        [JsonPropertyName("metadata")]
        public MetadataScheduleDto? Metadata { get; set; }
    }
}
