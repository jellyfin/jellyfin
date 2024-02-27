using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Request schedule for channel dto.
    /// </summary>
    public class RequestScheduleForChannelDto
    {
        /// <summary>
        /// Gets or sets the station id.
        /// </summary>
        [JsonPropertyName("stationID")]
        public string? StationId { get; set; }

        /// <summary>
        /// Gets or sets the list of dates.
        /// </summary>
        [JsonPropertyName("date")]
        public IReadOnlyList<string> Date { get; set; } = Array.Empty<string>();
    }
}
