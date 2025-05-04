using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// Client capabilities dto.
/// </summary>
public class ClientCapabilitiesDto
{
    /// <summary>
    /// Gets or sets the list of playable media types.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<MediaType> PlayableMediaTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of supported commands.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<GeneralCommandType> SupportedCommands { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether session supports media control.
    /// </summary>
    public bool SupportsMediaControl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether session supports a persistent identifier.
    /// </summary>
    public bool SupportsPersistentIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the device profile.
    /// </summary>
    public DeviceProfile? DeviceProfile { get; set; }

    /// <summary>
    /// Gets or sets the app store url.
    /// </summary>
    public string? AppStoreUrl { get; set; }

    /// <summary>
    /// Gets or sets the icon url.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Convert the dto to the full <see cref="ClientCapabilities"/> model.
    /// </summary>
    /// <returns>The converted <see cref="ClientCapabilities"/> model.</returns>
    public ClientCapabilities ToClientCapabilities()
    {
        return new ClientCapabilities
        {
            PlayableMediaTypes = PlayableMediaTypes,
            SupportedCommands = SupportedCommands,
            SupportsMediaControl = SupportsMediaControl,
            SupportsPersistentIdentifier = SupportsPersistentIdentifier,
            DeviceProfile = DeviceProfile,
            AppStoreUrl = AppStoreUrl,
            IconUrl = IconUrl
        };
    }
}
