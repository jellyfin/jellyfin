using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.LiveTvDtos;

/// <summary>
/// Set channel mapping dto.
/// </summary>
public class SetChannelMappingDto
{
    /// <summary>
    /// Gets or sets the provider id.
    /// </summary>
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tuner channel id.
    /// </summary>
    [Required]
    public string TunerChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider channel id.
    /// </summary>
    [Required]
    public string ProviderChannelId { get; set; } = string.Empty;
}
