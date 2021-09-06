#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
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
        public List<Description100Dto> Description100 { get; set; }

        /// <summary>
        /// Gets or sets the list of description1000.
        /// </summary>
        [JsonPropertyName("description1000")]
        public List<Description1000Dto> Description1000 { get; set; }
    }
}
