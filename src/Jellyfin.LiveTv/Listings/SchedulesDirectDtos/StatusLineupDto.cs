using System;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// One lineup entry of the /status response.
/// </summary>
public class StatusLineupDto
{
    /// <summary>
    /// Gets or sets the lineup id.
    /// </summary>
    [JsonPropertyName("lineup")]
    public string? Lineup { get; set; }

    /// <summary>
    /// Gets or sets the lineup id as reported for deleted lineups, which use a different name.
    /// </summary>
    [JsonPropertyName("ID")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the time the lineup was last modified. A client only has to download the
    /// lineup again when this is newer than the copy it holds.
    /// </summary>
    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lineup has been deleted at the headend.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
}
