#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents the server configuration.
    /// </summary>
    public class ServerConfiguration : BaseApplicationConfiguration
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
        /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
        /// </summary>
        public ServerConfiguration()
        {
            MetadataOptions = new[]
            {
                new MetadataOptions()
                {
                    ItemType = "Book"
                },
                new MetadataOptions()
                {
                    ItemType = "Movie"
                },
                new MetadataOptions
                {
                    ItemType = "MusicVideo",
                    DisabledMetadataFetchers = new[] { "The Open Movie Database" },
                    DisabledImageFetchers = new[] { "The Open Movie Database" }
                },
                new MetadataOptions
                {
                    ItemType = "Series",
                },
                new MetadataOptions
                {
                    ItemType = "MusicAlbum",
                    DisabledMetadataFetchers = new[] { "TheAudioDB" }
                },
                new MetadataOptions
                {
                    ItemType = "MusicArtist",
                    DisabledMetadataFetchers = new[] { "TheAudioDB" }
                },
                new MetadataOptions
                {
                    ItemType = "BoxSet"
                },
                new MetadataOptions
                {
                    ItemType = "Season",
                },
                new MetadataOptions
                {
                    ItemType = "Episode",
                }
            };
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic port forwarding.
        /// </summary>
        public bool EnableUPnP { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable prometheus metrics exporting.
        /// </summary>
        public bool EnableMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets the public mapped port.
        /// </summary>
        /// <value>The public mapped port.</value>
        public int PublicPort { get; set; } = DefaultHttpPort;

        /// <summary>
        /// Gets or sets a value indicating whether the http port should be mapped as part of UPnP automatic port forwarding.
        /// </summary>
        public bool UPnPCreateHttpPortMap { get; set; } = false;

        /// <summary>
        /// Gets or sets client udp port range.
        /// </summary>
        public string UDPPortRange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether IPV6 capability is enabled.
        /// </summary>
        public bool EnableIPV6 { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether IPV4 capability is enabled.
        /// </summary>
        public bool EnableIPV4 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether detailed ssdp logs are sent to the console/log.
        /// "Emby.Dlna": "Debug" must be set in logging.default.json for this property to work.
        /// </summary>
        public bool EnableSSDPTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether an IP address is to be used to filter the detailed ssdp logs that are being sent to the console/log.
        /// If the setting "Emby.Dlna": "Debug" msut be set in logging.default.json for this property to work.
        /// </summary>
        public string SSDPTracingFilter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of times SSDP UDP messages are sent.
        /// </summary>
        public int UDPSendCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the delay between each groups of SSDP messages (in ms).
        /// </summary>
        public int UDPSendDelay { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether address names that match <see cref="VirtualInterfaceNames"/> should be Ignore for the purposes of binding.
        /// </summary>
        public bool IgnoreVirtualInterfaces { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the interfaces that should be ignored. The list can be comma separated. <seealso cref="IgnoreVirtualInterfaces"/>.
        /// </summary>
        public string VirtualInterfaceNames { get; set; } = "vEthernet*";

        /// <summary>
        /// Gets or sets the time (in seconds) between the pings of SSDP gateway monitor.
        /// </summary>
        public int GatewayMonitorPeriod { get; set; } = 60;

        /// <summary>
        /// Gets a value indicating whether multi-socket binding is available.
        /// </summary>
        public bool EnableMultiSocketBinding { get; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether all IPv6 interfaces should be treated as on the internal network.
        /// Depending on the address range implemented ULA ranges might not be used.
        /// </summary>
        public bool TrustAllIP6Interfaces { get; set; } = false;

        /// <summary>
        /// Gets or sets the ports that HDHomerun uses.
        /// </summary>
        public string HDHomerunPortRange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets PublishedServerUri to advertise for specific subnets.
        /// </summary>
        public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether Autodiscovery tracing is enabled.
        /// </summary>
        public bool AutoDiscoveryTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Autodiscovery is enabled.
        /// </summary>
        public bool AutoDiscovery { get; set; } = true;

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
        public bool EnableHttps { get; set; } = false;

        public bool EnableNormalizedItemByNameIds { get; set; } = true;

        /// <summary>
        /// Gets or sets the filesystem path of an X.509 certificate to use for SSL.
        /// </summary>
        public string CertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password required to access the X.509 certificate data in the file specified by <see cref="CertificatePath"/>.
        /// </summary>
        public string CertificatePassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is port authorized.
        /// </summary>
        /// <value><c>true</c> if this instance is port authorized; otherwise, <c>false</c>.</value>
        public bool IsPortAuthorized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether quick connect is available for use on this server.
        /// </summary>
        public bool QuickConnectAvailable { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether access outside of the LAN is permitted.
        /// </summary>
        public bool EnableRemoteAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [enable case sensitive item ids].
        /// </summary>
        /// <value><c>true</c> if [enable case sensitive item ids]; otherwise, <c>false</c>.</value>
        public bool EnableCaseSensitiveItemIds { get; set; } = true;

        public bool DisableLiveTvChannelUserDataName { get; set; } = true;

        /// <summary>
        /// Gets or sets the metadata path.
        /// </summary>
        /// <value>The metadata path.</value>
        public string MetadataPath { get; set; } = string.Empty;

        public string MetadataNetworkPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        public string PreferredMetadataLanguage { get; set; } = "en";

        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        public string MetadataCountryCode { get; set; } = "US";

        /// <summary>
        /// Gets or sets characters to be replaced with a ' ' in strings to create a sort name.
        /// </summary>
        /// <value>The sort replace characters.</value>
        public string[] SortReplaceCharacters { get; set; } = new[] { ".", "+", "%" };

        /// <summary>
        /// Gets or sets characters to be removed from strings to create a sort name.
        /// </summary>
        /// <value>The sort remove characters.</value>
        public string[] SortRemoveCharacters { get; set; } = new[] { ",", "&", "-", "{", "}", "'" };

        /// <summary>
        /// Gets or sets words to be removed from strings to create a sort name.
        /// </summary>
        /// <value>The sort remove words.</value>
        public string[] SortRemoveWords { get; set; } = new[] { "the", "a", "an" };

        /// <summary>
        /// Gets or sets the minimum percentage of an item that must be played in order for playstate to be updated.
        /// </summary>
        /// <value>The min resume PCT.</value>
        public int MinResumePct { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum percentage of an item that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
        /// </summary>
        /// <value>The max resume PCT.</value>
        public int MaxResumePct { get; set; } = 90;

        /// <summary>
        /// Gets or sets the minimum duration that an item must have in order to be eligible for playstate updates..
        /// </summary>
        /// <value>The min resume duration seconds.</value>
        public int MinResumeDurationSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the minimum minutes of a book that must be played in order for playstate to be updated.
        /// </summary>
        /// <value>The min resume in minutes.</value>
        public int MinAudiobookResume { get; set; } = 5;

        /// <summary>
        /// Gets or sets the remaining minutes of a book that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
        /// </summary>
        /// <value>The remaining time in minutes.</value>
        public int MaxAudiobookResume { get; set; } = 5;

        /// <summary>
        /// Gets or sets the delay in seconds that we will wait after a file system change to try and discover what has been added/removed
        /// Some delay is necessary with some items because their creation is not atomic.  It involves the creation of several
        /// different directories and files.
        /// </summary>
        /// <value>The file watcher delay.</value>
        public int LibraryMonitorDelay { get; set; } = 60;

        /// <summary>
        /// Gets or sets a value indicating whether [enable dashboard response caching].
        /// Allows potential contributors without visual studio to modify production dashboard code and test changes.
        /// </summary>
        /// <value><c>true</c> if [enable dashboard response caching]; otherwise, <c>false</c>.</value>
        public bool EnableDashboardResponseCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the image saving convention.
        /// </summary>
        /// <value>The image saving convention.</value>
        public ImageSavingConvention ImageSavingConvention { get; set; }

        public MetadataOptions[] MetadataOptions { get; set; }

        public bool SkipDeserializationForBasicTypes { get; set; } = true;

        public string ServerName { get; set; } = string.Empty;

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
                if (value[value.Length - 1] == '/')
                {
                    // If baseUrl was configured with a trailing slash, remove it for consistency
                    value = value.Remove(value.Length - 1);
                }

                _baseUrl = value;
            }
        }

        public string UICulture { get; set; } = "en-US";

        public bool SaveMetadataHidden { get; set; } = false;

        public NameValuePair[] ContentTypes { get; set; } = Array.Empty<NameValuePair>();

        public int RemoteClientBitrateLimit { get; set; } = 0;

        public bool EnableFolderView { get; set; } = false;

        public bool EnableGroupingIntoCollections { get; set; } = false;

        public bool DisplaySpecialsWithinSeasons { get; set; } = true;

        /// <summary>
        /// Gets or sets the subnets that are deemed to make up the LAN.
        /// </summary>
        public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the interface addresses which Jellyfin will bind to. If empty, all interfaces will be used.
        /// </summary>
        public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();

        public string[] CodecsUsed { get; set; } = Array.Empty<string>();

        public List<RepositoryInfo> PluginRepositories { get; set; } = new List<RepositoryInfo>();

        public bool EnableExternalContentInSuggestions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the server should force connections over HTTPS.
        /// </summary>
        public bool RequireHttps { get; set; } = false;

        public bool EnableNewOmdbSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter for remote IP connectivity. Used in conjuntion with <seealso cref="IsRemoteIPFilterBlacklist"/>.
        /// </summary>
        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether <seealso cref="RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist.
        /// </summary>
        public bool IsRemoteIPFilterBlacklist { get; set; } = false;

        public int ImageExtractionTimeoutMs { get; set; } = 0;

        public PathSubstitution[] PathSubstitutions { get; set; } = Array.Empty<PathSubstitution>();

        public string[] UninstalledPlugins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether slow server responses should be logged as a warning.
        /// </summary>
        public bool EnableSlowResponseWarning { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold for the slow response time warning in ms.
        /// </summary>
        public long SlowResponseThresholdMs { get; set; } = 500;

        /// <summary>
        /// Gets or sets the cors hosts.
        /// </summary>
        public string[] CorsHosts { get; set; } = new[] { "*" };

        /// <summary>
        /// Gets or sets the known proxies.
        /// </summary>
        public string[] KnownProxies { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the number of days we should retain activity logs.
        /// </summary>
        public int? ActivityLogRetentionDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets the how the library scan fans out.
        /// </summary>
        public int LibraryScanFanoutConcurrency { get; set; }

        /// <summary>
        /// Gets or sets the how many metadata refreshes can run concurrently.
        /// </summary>
        public int LibraryMetadataRefreshConcurrency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether older plugins should automatically be deleted from the plugin folder.
        /// </summary>
        public bool RemoveOldPlugins { get; set; }
    }
}
