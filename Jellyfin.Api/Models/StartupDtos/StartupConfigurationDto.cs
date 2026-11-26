using System;

namespace Jellyfin.Api.Models.StartupDtos;

/// <summary>
/// The startup configuration DTO.
/// </summary>
[Obsolete("Use ServerConfiguration")]
public class StartupConfigurationDto
{
    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Gets or sets UI language culture.
    /// </summary>
    public string? UICulture { get; set; }

    /// <summary>
    /// Gets or sets the metadata country code.
    /// </summary>
    public string? MetadataCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the preferred language for the metadata.
    /// </summary>
    public string? PreferredMetadataLanguage { get; set; }
}
