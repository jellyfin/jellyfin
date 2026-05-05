#pragma warning disable CA1819

using System;

namespace MediaBrowser.Common.Net;

/// <summary>
/// The network configuration DTO for API use.
/// </summary>
public class NetworkConfigurationDto
{
    /// <summary>
    /// Gets or sets a value used to specify the URL prefix that your Jellyfin instance can be accessed at.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use HTTPS.
    /// </summary>
    public bool EnableHttps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the server should force connections over HTTPS.
    /// </summary>
    public bool RequireHttps { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path of an X.509 certificate to use for SSL.
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password required to access the X.509 certificate data in the file specified by <see cref="CertificatePath"/>.
    /// </summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal HTTP server port.
    /// </summary>
    public int InternalHttpPort { get; set; } = NetworkConfiguration.DefaultHttpPort;

    /// <summary>
    /// Gets or sets the internal HTTPS server port.
    /// </summary>
    public int InternalHttpsPort { get; set; } = NetworkConfiguration.DefaultHttpsPort;

    /// <summary>
    /// Gets or sets the public HTTP port.
    /// </summary>
    public int PublicHttpPort { get; set; } = NetworkConfiguration.DefaultHttpPort;

    /// <summary>
    /// Gets or sets the public HTTPS port.
    /// </summary>
    public int PublicHttpsPort { get; set; } = NetworkConfiguration.DefaultHttpsPort;

    /// <summary>
    /// Gets or sets a value indicating whether Autodiscovery is enabled.
    /// </summary>
    public bool AutoDiscovery { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic port forwarding.
    /// </summary>
    public bool EnableUPnP { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether IPv4 is enabled.
    /// </summary>
    public bool EnableIPv4 { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether IPv6 is enabled.
    /// </summary>
    public bool EnableIPv6 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether access from outside of the LAN is permitted.
    /// </summary>
    public bool EnableRemoteAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the subnets that are deemed to make up the LAN.
    /// </summary>
    public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the interface addresses which Jellyfin will bind to. If empty, all interfaces will be used.
    /// </summary>
    public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the known proxies.
    /// </summary>
    public string[] KnownProxies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether address names that match <see cref="VirtualInterfaceNames"/> should be ignored for the purposes of binding.
    /// </summary>
    public bool IgnoreVirtualInterfaces { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating the interface name prefixes that should be ignored.
    /// </summary>
    public string[] VirtualInterfaceNames { get; set; } = new string[] { "veth" };

    /// <summary>
    /// Gets or sets a value indicating whether the published server uri is based on information in HTTP requests.
    /// </summary>
    public bool EnablePublishedServerUriByRequest { get; set; } = false;

    /// <summary>
    /// Gets or sets PublishedServerUri to advertise for specific subnets.
    /// </summary>
    public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the filter for remote IP connectivity. Used in conjunction with <seealso cref="IsRemoteIPFilterBlacklist"/>.
    /// </summary>
    public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether <seealso cref="RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist.
    /// </summary>
    public bool IsRemoteIPFilterBlacklist { get; set; }
}
