using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Lineups dto.
    /// </summary>
    public class LineupsDto
    {
        /// <summary>
        /// Gets or sets the response code.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the server id.
        /// </summary>
        [JsonPropertyName("serverID")]
        public string? ServerId { get; set; }

        /// <summary>
        /// Gets or sets the datetime.
        /// </summary>
        [JsonPropertyName("datetime")]
        public DateTime? LineupTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the list of lineups.
        /// </summary>
        [JsonPropertyName("lineups")]
        public IReadOnlyList<LineupDto> Lineups { get; set; } = Array.Empty<LineupDto>();
    }
}
