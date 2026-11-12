using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.ShareLinks;

/// <summary>
/// Request body for creating a scoped share link.
/// </summary>
public class CreateShareLinkRequestDto
{
    /// <summary>
    /// Gets or sets the absolute expiration timestamp in UTC, if any.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the lifetime in seconds starting from the first access, if any.
    /// </summary>
    [Range(60, 7776000)]
    public int? DurationSeconds { get; set; }
}
