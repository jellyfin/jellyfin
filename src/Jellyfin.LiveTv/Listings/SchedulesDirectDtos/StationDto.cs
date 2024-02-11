using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Station dto.
    /// </summary>
    public class StationDto
    {
        /// <summary>
        /// Gets or sets the station id.
        /// </summary>
        [JsonPropertyName("stationID")]
        public string? StationId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the callsign.
        /// </summary>
        [JsonPropertyName("callsign")]
        public string? Callsign { get; set; }

        /// <summary>
        /// Gets or sets the broadcast language.
        /// </summary>
        [JsonPropertyName("broadcastLanguage")]
        public IReadOnlyList<string> BroadcastLanguage { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the description language.
        /// </summary>
        [JsonPropertyName("descriptionLanguage")]
        public IReadOnlyList<string> DescriptionLanguage { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the broadcaster.
        /// </summary>
        [JsonPropertyName("broadcaster")]
        public BroadcasterDto? Broadcaster { get; set; }

        /// <summary>
        /// Gets or sets the affiliate.
        /// </summary>
        [JsonPropertyName("affiliate")]
        public string? Affiliate { get; set; }

        /// <summary>
        /// Gets or sets the logo.
        /// </summary>
        [JsonPropertyName("logo")]
        public LogoDto? Logo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is commercial free.
        /// </summary>
        [JsonPropertyName("isCommercialFree")]
        public bool? IsCommercialFree { get; set; }
    }
}
