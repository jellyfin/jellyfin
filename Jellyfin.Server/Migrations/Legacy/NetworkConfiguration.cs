#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace Jellyfin.Server.Migrations.Legacy
{
    /// <summary>
    /// Defines the <see cref="NetworkConfiguration" />.
    /// This is a point in time snapshot when the network configuration (network.xml) was removed from system.xml.
    /// </summary>
    public class NetworkConfiguration
    {
        /// <summary>
        /// The default value for <see cref="HttpServerPortNumber"/>.
        /// </summary>
        public const int DefaultHttpPort = 8096;

        /// <summary>
        /// The default value for <see cref="PublicHttpsPort"/> and <see cref="HttpsPortNumber"/>.
        /// </summary>
        public const int DefaultHttpsPort = 8920;

        private string _baseUrl = string.Empty;

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
        /// Gets or sets a value used to specify the URL prefix that your Jellyfin instance can be accessed at.
        /// </summary>
        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                // Normalize the start of the string
                if (string.IsNullOrWhiteSpace(value))
                {
                    // If baseUrl is empty, set an empty prefix string
                    _baseUrl = string.Empty;
                    return;
                }

                if (value[0] != '/')
                {
                    // If baseUrl was not configured with a leading slash, append one for consistency
                    value = "/" + value;
                }

                // Normalize the end of the string
                if (value[^1] == '/')
                {
                    // If baseUrl was configured with a trailing slash, remove it for consistency
                    value = value.Remove(value.Length - 1);
                }

                _baseUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the public HTTPS port.
        /// </summary>
        /// <value>The public HTTPS port.</value>
        public int PublicHttpsPort { get; set; } = DefaultHttpsPort;

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        public int HttpServerPortNumber { get; set; } = DefaultHttpPort;

        /// <summary>
        /// Gets or sets the HTTPS server port number.
        /// </summary>
        /// <value>The HTTPS server port number.</value>
        public int HttpsPortNumber { get; set; } = DefaultHttpsPort;

        /// <summary>
        /// Gets or sets a value indicating whether to use HTTPS.
        /// </summary>
        /// <remarks>
        /// In order for HTTPS to be used, in addition to setting this to true, valid values must also be
        /// provided for <see cref="CertificatePath"/> and <see cref="CertificatePassword"/>.
        /// </remarks>
        public bool EnableHttps { get; set; }

        /// <summary>
        /// Gets or sets the public mapped port.
        /// </summary>
        /// <value>The public mapped port.</value>
        public int PublicPort { get; set; } = DefaultHttpPort;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IPV6 capability.
        /// </summary>
        public bool EnableIPV6 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IPV4 capability.
        /// </summary>
        public bool EnableIPV4 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether address names that match <see cref="VirtualInterfaceNames"/> should be Ignore for the purposes of binding.
        /// </summary>
        public bool IgnoreVirtualInterfaces { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the interfaces that should be ignored. The list can be comma separated. <seealso cref="IgnoreVirtualInterfaces"/>.
        /// </summary>
        public string VirtualInterfaceNames { get; set; } = "vEthernet*";

        /// <summary>
        /// Gets or sets a value indicating whether all IPv6 interfaces should be treated as on the internal network.
        /// Depending on the address range implemented ULA ranges might not be used.
        /// </summary>
        public bool TrustAllIP6Interfaces { get; set; }

        /// <summary>
        /// Gets or sets the PublishedServerUriBySubnet
        /// Gets or sets PublishedServerUri to advertise for specific subnets.
        /// </summary>
        public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the filter for remote IP connectivity. Used in conjuntion with <seealso cref="IsRemoteIPFilterBlacklist"/>.
        /// </summary>
        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether <seealso cref="RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist.
        /// </summary>
        public bool IsRemoteIPFilterBlacklist { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic port forwarding.
        /// </summary>
        public bool EnableUPnP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access outside of the LAN is permitted.
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
        /// Gets or sets the known proxies. If the proxy is a network, it's added to the KnownNetworks.
        /// </summary>
        public string[] KnownProxies { get; set; } = Array.Empty<string>();
    }
}
