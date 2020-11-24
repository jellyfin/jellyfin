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
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.DefaultHttpPort instead.", false)]
        public const int DefaultHttpPort = 8096;

        /// <summary>
        /// The default value for <see cref="ServerConfiguration.PublicHttpsPort"/> and <see cref="ServerConfiguration.HttpsPortNumber"/>.
        /// </summary>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.DefaultHttpsPort instead.", false)]
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
                    DisabledMetadataFetchers = new[] { "TheMovieDb" },
                    DisabledImageFetchers = new[] { "TheMovieDb" }
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
                    DisabledMetadataFetchers = new[] { "TheMovieDb" },
                },
                new MetadataOptions
                {
                    ItemType = "Episode",
                    DisabledMetadataFetchers = new[] { "The Open Movie Database", "TheMovieDb" },
                    DisabledImageFetchers = new[] { "The Open Movie Database", "TheMovieDb" }
                }
            };
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable prometheus metrics exporting.
        /// </summary>
        public bool EnableMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets the public HTTPS port.
        /// </summary>
        /// <value>The public HTTPS port.</value>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.PublicHttpsPort instead.", false)]
        public int PublicHttpsPort { get; set; } = DefaultHttpsPort;

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.DefaultHttpPort instead.", false)]
        public int HttpServerPortNumber { get; set; } = DefaultHttpPort;

        /// <summary>
        /// Gets or sets the HTTPS server port number.
        /// </summary>
        /// <value>The HTTPS server port number.</value>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.HttpsPortNumber instead.", false)]
        public int HttpsPortNumber { get; set; } = DefaultHttpsPort;

        /// <summary>
        /// Gets or sets a value indicating whether to use HTTPS.
        /// </summary>
        /// <remarks>
        /// In order for HTTPS to be used, in addition to setting this to true, valid values must also be
        /// provided for <see cref="CertificatePath"/> and <see cref="CertificatePassword"/>.
        /// </remarks>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.EnableHttps instead.", false)]
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
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.EnabledRemoteAccess instead.", false)]
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
        public string PreferredMetadataLanguage { get; set; } = string.Empty;

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

        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.BaseUrl instead.", false)]
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
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.LocalNetworkSubnets instead.", false)]
        public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the interface addresses which Jellyfin will bind to. If empty, all interfaces will be used.
        /// </summary>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.LocalNetworkAddresses instead.", false)]
        public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();

        public string[] CodecsUsed { get; set; } = Array.Empty<string>();

        public List<RepositoryInfo> PluginRepositories { get; set; } = new List<RepositoryInfo>();

        public bool EnableExternalContentInSuggestions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the server should force connections over HTTPS.
        /// </summary>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.RequireHttps instead.", false)]
        public bool RequireHttps { get; set; } = false;

        public bool EnableNewOmdbSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter for remote IP connectivity. Used in conjunction with <seealso cref="ServerConfiguration.IsRemoteIPFilterBlacklist"/>.
        /// </summary>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.RemoteIPFilter instead.", false)]
        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether <seealso cref="ServerConfiguration.RemoteIPFilter"/> contains a blacklist or a whitelist. Default is a whitelist.
        /// </summary>
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.IsRemoteIPFilteringBlacklisted instead.", false)]
        public bool IsRemoteIPFilterBlacklist { get; set; } = false;

        public int ImageExtractionTimeoutMs { get; set; } = 0;

        public PathSubstitution[] PathSubstitutions { get; set; } = Array.Empty<PathSubstitution>();

        public bool EnableSimpleArtistDetection { get; set; } = false;

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
        [ObsoleteAttribute("This property is obsolete. Use NetworkConfiguration.KnownProxies instead.", false)]
        public string[] KnownProxies { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the number of days we should retain activity logs.
        /// </summary>
        public int? ActivityLogRetentionDays { get; set; } = 30;
    }
}
