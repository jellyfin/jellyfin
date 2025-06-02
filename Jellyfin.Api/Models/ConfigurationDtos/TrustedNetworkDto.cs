using System.Collections.Generic;

namespace Jellyfin.Api.Models.ConfigurationDtos;

/// <summary>
/// Represents the configuration for trusted networks, including local network subnets and remote IP filtering options.
/// </summary>
public class TrustedNetworkDto
{
    /// <summary>
    /// Gets or sets the subnets that are deemed to make up the LAN.
    /// </summary>
    public IReadOnlyList<string>? LocalNetworkSubnets { get; set; }

    /// <summary>
    /// Gets or sets the filter for remote IP connectivity. Used in conjunction with <seealso cref="IsRemoteIPFilterBlacklist"/>.
    /// </summary>
    public IReadOnlyList<string>? RemoteIPFilter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <seealso cref="RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist.
    /// </summary>
    public bool? IsRemoteIPFilterBlacklist { get; set; }
}
