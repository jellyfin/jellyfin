using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// The /status response.
/// </summary>
public class StatusDto
{
    /// <summary>
    /// Gets or sets the account details.
    /// </summary>
    [JsonPropertyName("account")]
    public StatusAccountDto? Account { get; set; }

    /// <summary>
    /// Gets or sets the lineups on the account, with the time each was last modified.
    /// </summary>
    [JsonPropertyName("lineups")]
    public IReadOnlyList<StatusLineupDto> Lineups { get; set; } = [];

    /// <summary>
    /// Gets or sets the system status entries. The spec does not define their order, so every
    /// entry has to be inspected.
    /// </summary>
    [JsonPropertyName("systemStatus")]
    public IReadOnlyList<SystemStatusDto> SystemStatus { get; set; } = [];

    /// <summary>
    /// Gets or sets the response code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
}
