#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace Jellyfin.Networking.Configuration
{
    /// <summary>
    /// Defines the <see cref="NetworkConfiguration" />.
    /// </summary>
    public class NetworkConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the http port should be mapped as part of UPnP automatic port forwarding..
        /// </summary>
        public bool UPnPCreateHttpPortMap { get; set; } = false;

        /// <summary>
        /// Gets or sets the UDPPortRange
        /// Gets or sets client udp port range..
        /// </summary>
        public string UDPPortRange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IPV6 capability..
        /// </summary>
        public bool EnableIPV6 { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IPV4 capability..
        /// </summary>
        public bool EnableIPV4 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether detailed ssdp logs are sent to the console/log.
        /// "Emby.Dlna": "Debug" must be set in logging.default.json for this property to work..
        /// </summary>
        public bool EnableSSDPTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets the SSDPTracingFilter
        /// Gets or sets a value indicating whether an IP address is to be used to filter the detailed ssdp logs that are being sent to the console/log.
        /// If the setting "Emby.Dlna": "Debug" msut be set in logging.default.json for this property to work..
        /// </summary>
        public string SSDPTracingFilter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of times SSDP UDP messages are sent..
        /// </summary>
        public int UDPSendCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the delay between each groups of SSDP messages (in ms)..
        /// </summary>
        public int UDPSendDelay { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether address names that match <see cref="VirtualInterfaceNames"/> should be Ignore for the purposes of binding..
        /// </summary>
        public bool IgnoreVirtualInterfaces { get; set; } = true;

        /// <summary>
        /// Gets or sets the VirtualInterfaceNames
        /// Gets or sets a value indicating the interfaces that should be ignored. The list can be comma separated. <seealso cref="IgnoreVirtualInterfaces"/>..
        /// </summary>
        public string VirtualInterfaceNames { get; set; } = "vEthernet*";

        /// <summary>
        /// Gets or sets the time (in seconds) between the pings of SSDP gateway monitor..
        /// </summary>
        public int GatewayMonitorPeriod { get; set; } = 60;

        /// <summary>
        /// Gets a value indicating whether multi-socket binding is available..
        /// </summary>
        public bool EnableMultiSocketBinding { get; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether all IPv6 interfaces should be treated as on the internal network.
        /// Depending on the address range implemented ULA ranges might not be used..
        /// </summary>
        public bool TrustAllIP6Interfaces { get; set; } = false;

        /// <summary>
        /// Gets or sets the ports that HDHomerun uses..
        /// </summary>
        public string HDHomerunPortRange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the PublishedServerUriBySubnet
        /// Gets or sets PublishedServerUri to advertise for specific subnets..
        /// </summary>
        public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether Autodiscovery tracing is enabled..
        /// </summary>
        public bool AutoDiscoveryTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Autodiscovery is enabled..
        /// </summary>
        public bool AutoDiscovery { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter for remote IP connectivity. Used in conjuntion with <seealso cref="IsRemoteIPFilterBlacklist"/>..
        /// </summary>
        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether <seealso cref="RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist..
        /// </summary>
        public bool IsRemoteIPFilterBlacklist { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic port forwarding..
        /// </summary>
        public bool EnableUPnP { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether access outside of the LAN is permitted..
        /// </summary>
        public bool EnableRemoteAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets the subnets that are deemed to make up the LAN..
        /// </summary>
        public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the interface addresses which Jellyfin will bind to. If empty, all interfaces will be used..
        /// </summary>
        public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();
    }
}
