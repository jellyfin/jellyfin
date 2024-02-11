using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Show image dto.
    /// </summary>
    public class ShowImagesDto
    {
        /// <summary>
        /// Gets or sets the program id.
        /// </summary>
        [JsonPropertyName("programID")]
        public string? ProgramId { get; set; }

        /// <summary>
        /// Gets or sets the list of data.
        /// </summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<ImageDataDto> Data { get; set; } = Array.Empty<ImageDataDto>();
    }
}
