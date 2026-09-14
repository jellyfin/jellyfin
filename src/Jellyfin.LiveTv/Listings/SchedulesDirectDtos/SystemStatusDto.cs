using System;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// One entry of the system status.
/// </summary>
public class SystemStatusDto
{
    /// <summary>
    /// Gets or sets the time the status was posted.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the status. "Offline" means the client must disconnect for an hour.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the message describing the status.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
