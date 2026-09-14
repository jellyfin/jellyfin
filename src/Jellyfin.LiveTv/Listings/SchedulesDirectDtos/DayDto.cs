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
        /// Gets or sets the error code. Set when this entry is a per-station error
        /// (for example SCHEDULE_RANGE_EXCEEDED) instead of a schedule.
        /// </summary>
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        /// <summary>
        /// Gets or sets the error message that goes with <see cref="Code"/>.
        /// </summary>
        /// <remarks>
        /// Not every error entry carries one; STATIONID_DELETED only sets <see cref="Response"/>.
        /// </remarks>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the symbolic name of <see cref="Code"/>, for example STATIONID_DELETED.
        /// </summary>
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        /// <summary>
        /// Gets or sets the earliest time a queued schedule (SCHEDULE_QUEUED) may be requested
        /// again. The spec requires the client to wait at least this long.
        /// </summary>
        [JsonPropertyName("retryTime")]
        public DateTime? RetryTime { get; set; }

        /// <summary>
        /// Gets or sets the metadata schedule.
        /// </summary>
        [JsonPropertyName("metadata")]
        public MetadataScheduleDto? Metadata { get; set; }
    }
}
