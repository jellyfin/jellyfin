using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Descriptions program dto.
    /// </summary>
    public class DescriptionsProgramDto
    {
        /// <summary>
        /// Gets or sets the list of description 100.
        /// </summary>
        [JsonPropertyName("description100")]
        public IReadOnlyList<Description100Dto> Description100 { get; set; } = Array.Empty<Description100Dto>();

        /// <summary>
        /// Gets or sets the list of description1000.
        /// </summary>
        [JsonPropertyName("description1000")]
        public IReadOnlyList<Description1000Dto> Description1000 { get; set; } = Array.Empty<Description1000Dto>();
    }
}
