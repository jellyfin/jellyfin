#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    public class ShowImagesDto
    {
        /// <summary>
        /// Gets or sets the program id.
        /// </summary>
        [JsonPropertyName("programID")]
        public string ProgramId { get; set; }

        /// <summary>
        /// Gets or sets the list of data.
        /// </summary>
        [JsonPropertyName("data")]
        public List<ImageDataDto> Data { get; set; }
    }
}
