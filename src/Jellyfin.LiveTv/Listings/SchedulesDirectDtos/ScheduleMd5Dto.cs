using System;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// The md5 and last modified date of one station's schedule for one day.
/// </summary>
public class ScheduleMd5Dto
{
    /// <summary>
    /// Gets or sets the error code. Non-zero when the day is unavailable.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the message that goes with <see cref="Code"/>.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the time the schedule was last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the md5 of the schedule. A change means the schedule must be downloaded again.
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }
}
