#nullable disable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Day dto.
    /// </summary>
    public class DayDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DayDto"/> class.
        /// </summary>
        public DayDto()
        {
            Programs = new List<ProgramDto>();
        }

        /// <summary>
        /// Gets or sets the station id.
        /// </summary>
        [JsonPropertyName("stationID")]
        public string StationId { get; set; }

        /// <summary>
        /// Gets or sets the list of programs.
        /// </summary>
        [JsonPropertyName("programs")]
        public List<ProgramDto> Programs { get; set; }

        /// <summary>
        /// Gets or sets the metadata schedule.
        /// </summary>
        [JsonPropertyName("metadata")]
        public MetadataScheduleDto Metadata { get; set; }
    }
}
