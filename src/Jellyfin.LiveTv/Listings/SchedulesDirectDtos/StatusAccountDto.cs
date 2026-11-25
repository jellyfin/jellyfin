using System;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// The account block of the /status response.
/// </summary>
public class StatusAccountDto
{
    /// <summary>
    /// Gets or sets the time the subscription expires.
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of lineups the account may hold.
    /// </summary>
    [JsonPropertyName("maxLineups")]
    public int? MaxLineups { get; set; }
}
