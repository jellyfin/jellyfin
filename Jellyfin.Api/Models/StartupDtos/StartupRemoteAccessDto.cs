using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.StartupDtos;

/// <summary>
/// Startup remote access dto.
/// </summary>
public class StartupRemoteAccessDto
{
    /// <summary>
    /// Gets or sets a value indicating whether enable remote access.
    /// </summary>
    [Required]
    public bool EnableRemoteAccess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enable automatic port mapping.
    /// </summary>
    [Required]
    [Obsolete("No longer supported")]
    public bool EnableAutomaticPortMapping { get; set; }
}
