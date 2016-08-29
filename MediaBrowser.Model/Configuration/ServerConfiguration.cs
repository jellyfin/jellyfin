using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents the server configuration.
    /// </summary>
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable u pn p].
        /// </summary>
        /// <value><c>true</c> if [enable u pn p]; otherwise, <c>false</c>.</value>
        public bool EnableUPnP { get; set; }

        /// <summary>
        /// Gets or sets the public mapped port.
        /// </summary>
        /// <value>The public mapped port.</value>
        public int PublicPort { get; set; }

        /// <summary>
        /// Gets or sets the public HTTPS port.
        /// </summary>
        /// <value>The public HTTPS port.</value>
        public int PublicHttpsPort { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the HTTPS server port number.
        /// </summary>
        /// <value>The HTTPS server port number.</value>
        public int HttpsPortNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use HTTPS].
        /// </summary>
        /// <value><c>true</c> if [use HTTPS]; otherwise, <c>false</c>.</value>
        public bool EnableHttps { get; set; }

        /// <summary>
        /// Gets or sets the value pointing to the file system where the ssl certiifcate is located..
        /// </summary>
        /// <value>The value pointing to the file system where the ssl certiifcate is located..</value>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable internet providers].
        /// </summary>
        /// <value><c>true</c> if [enable internet providers]; otherwise, <c>false</c>.</value>
        public bool EnableInternetProviders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is port authorized.
        /// </summary>
        /// <value><c>true</c> if this instance is port authorized; otherwise, <c>false</c>.</value>
        public bool IsPortAuthorized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable case sensitive item ids].
        /// </summary>
        /// <value><c>true</c> if [enable case sensitive item ids]; otherwise, <c>false</c>.</value>
        public bool EnableCaseSensitiveItemIds { get; set; }

        /// <summary>
        /// Gets or sets the metadata path.
        /// </summary>
        /// <value>The metadata path.</value>
        public string MetadataPath { get; set; }

        public string LastVersion { get; set; }

        /// <summary>
        /// Gets or sets the display name of the season zero.
        /// </summary>
        /// <value>The display name of the season zero.</value>
        public string SeasonZeroDisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        public bool SaveLocalMeta { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        public string MetadataCountryCode { get; set; }

        /// <summary>
        /// Characters to be replaced with a ' ' in strings to create a sort name
        /// </summary>
        /// <value>The sort replace characters.</value>
        public string[] SortReplaceCharacters { get; set; }

        /// <summary>
        /// Characters to be removed from strings to create a sort name
        /// </summary>
        /// <value>The sort remove characters.</value>
        public string[] SortRemoveCharacters { get; set; }

        /// <summary>
        /// Words to be removed from strings to create a sort name
        /// </summary>
        /// <value>The sort remove words.</value>
        public string[] SortRemoveWords { get; set; }

        /// <summary>
        /// Gets or sets the minimum percentage of an item that must be played in order for playstate to be updated.
        /// </summary>
        /// <value>The min resume PCT.</value>
        public int MinResumePct { get; set; }

        /// <summary>
        /// Gets or sets the maximum percentage of an item that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
        /// </summary>
        /// <value>The max resume PCT.</value>
        public int MaxResumePct { get; set; }

        /// <summary>
        /// Gets or sets the minimum duration that an item must have in order to be eligible for playstate updates..
        /// </summary>
        /// <value>The min resume duration seconds.</value>
        public int MinResumeDurationSeconds { get; set; }

        /// <summary>
        /// The delay in seconds that we will wait after a file system change to try and discover what has been added/removed
        /// Some delay is necessary with some items because their creation is not atomic.  It involves the creation of several
        /// different directories and files.
        /// </summary>
        /// <value>The file watcher delay.</value>
        public int LibraryMonitorDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable dashboard response caching].
        /// Allows potential contributors without visual studio to modify production dashboard code and test changes.
        /// </summary>
        /// <value><c>true</c> if [enable dashboard response caching]; otherwise, <c>false</c>.</value>
        public bool EnableDashboardResponseCaching { get; set; }
        public bool EnableDashboardResourceMinification { get; set; }

        /// <summary>
        /// Allows the dashboard to be served from a custom path.
        /// </summary>
        /// <value>The dashboard source path.</value>
        public string DashboardSourcePath { get; set; }

        /// <summary>
        /// Gets or sets the image saving convention.
        /// </summary>
        /// <value>The image saving convention.</value>
        public ImageSavingConvention ImageSavingConvention { get; set; }

        public MetadataOptions[] MetadataOptions { get; set; }

        public bool EnableAutomaticRestart { get; set; }

        public PathSubstitution[] PathSubstitutions { get; set; }

        public string ServerName { get; set; }
        public string WanDdns { get; set; }

        public string UICulture { get; set; }

        public PeopleMetadataOptions PeopleMetadataOptions { get; set; }

        public bool SaveMetadataHidden { get; set; }

        public NameValuePair[] ContentTypes { get; set; }

        public int RemoteClientBitrateLimit { get; set; }

        public AutoOnOff EnableLibraryMonitor { get; set; }

        public int SharingExpirationDays { get; set; }

        public string[] Migrations { get; set; }

        public int MigrationVersion { get; set; }
        public int SchemaVersion { get; set; }
        public int SqliteCacheSize { get; set; }

        public bool DownloadImagesInAdvance { get; set; }

        public bool EnableAnonymousUsageReporting { get; set; }
        public bool EnableStandaloneMusicKeys { get; set; }
        public bool EnableLocalizedGuids { get; set; }
        public bool EnableFolderView { get; set; }
        public bool EnableGroupingIntoCollections { get; set; }
        public bool DisplaySpecialsWithinSeasons { get; set; }
        public bool DisplayCollectionsView { get; set; }
        public string[] LocalNetworkAddresses { get; set; }
        public string[] CodecsUsed { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
        /// </summary>
        public ServerConfiguration()
        {
            LocalNetworkAddresses = new string[] { };
            Migrations = new string[] { };
            CodecsUsed = new string[] { };
            SqliteCacheSize = 0;

            EnableLocalizedGuids = true;
            DisplaySpecialsWithinSeasons = true;

            ImageSavingConvention = ImageSavingConvention.Compatible;
            PublicPort = 8096;
            PublicHttpsPort = 8920;
            HttpServerPortNumber = 8096;
            HttpsPortNumber = 8920;
            EnableHttps = false;
            EnableDashboardResponseCaching = true;
            EnableDashboardResourceMinification = true;
            EnableAnonymousUsageReporting = true;

            EnableAutomaticRestart = true;

            EnableUPnP = true;
            SharingExpirationDays = 30;
            MinResumePct = 5;
            MaxResumePct = 90;

            // 5 minutes
            MinResumeDurationSeconds = 300;

            EnableLibraryMonitor = AutoOnOff.Auto;
            LibraryMonitorDelay = 60;

            EnableInternetProviders = true;

            PathSubstitutions = new PathSubstitution[] { };
            ContentTypes = new NameValuePair[] { };

            PreferredMetadataLanguage = "en";
            MetadataCountryCode = "US";

            SortReplaceCharacters = new[] { ".", "+", "%" };
            SortRemoveCharacters = new[] { ",", "&", "-", "{", "}", "'" };
            SortRemoveWords = new[] { "the", "a", "an" };

            SeasonZeroDisplayName = "Specials";

            UICulture = "en-us";

            PeopleMetadataOptions = new PeopleMetadataOptions();

            MetadataOptions = new[]
            {
                new MetadataOptions(1, 1280) {ItemType = "Book"},

                new MetadataOptions(1, 1280)
                {
                    ItemType = "Movie",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 1,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Art
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Disc
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        },

                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Banner
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Thumb
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Logo
                        }
                    }
                },

                new MetadataOptions(1, 1280)
                {
                    ItemType = "MusicVideo",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 1,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Art
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Disc
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        },

                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Banner
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Thumb
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Logo
                        }
                    }
                },

                new MetadataOptions(1, 1280)
                {
                    ItemType = "Series",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 1,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Art
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Banner
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Thumb
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Logo
                        }
                    }
                },

                new MetadataOptions(1, 1280)
                {
                    ItemType = "MusicAlbum",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 0,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Disc
                        }
                    }
                },

                new MetadataOptions(1, 1280)
                {
                    ItemType = "MusicArtist",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 1,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        // Don't download this by default
                        // They do look great, but most artists won't have them, which means a banner view isn't really possible
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Banner
                        },

                        // Don't download this by default
                        // Generally not used
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Art
                        },

                        // Don't download this by default
                        // Generally not used
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Logo
                        }
                    }
                },

                new MetadataOptions(1, 1280)
                {
                    ItemType = "BoxSet",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 1,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Thumb
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Logo
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Art
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Disc
                        },

                        // Don't download this by default as it's rarely used.
                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Banner
                        }
                    }
                },

                new MetadataOptions(0, 1280)
                {
                    ItemType = "Season",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 0,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        },

                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Banner
                        },

                        new ImageOption
                        {
                            Limit = 0,
                            Type = ImageType.Thumb
                        }
                    },
                    DisabledMetadataFetchers = new []{ "The Open Movie Database", "TheMovieDb" }
                },

                new MetadataOptions(0, 1280)
                {
                    ItemType = "Episode",
                    ImageOptions = new []
                    {
                        new ImageOption
                        {
                            Limit = 0,
                            MinWidth = 1280,
                            Type = ImageType.Backdrop
                        },

                        new ImageOption
                        {
                            Limit = 1,
                            Type = ImageType.Primary
                        }
                    },
                    DisabledMetadataFetchers = new []{ "The Open Movie Database" },
                    DisabledImageFetchers = new []{ "TheMovieDb" }
                }
            };
        }
    }
}