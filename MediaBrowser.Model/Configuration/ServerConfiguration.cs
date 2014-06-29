using MediaBrowser.Model.Weather;
using System;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents the server configuration.
    /// </summary>
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets the zip code to use when displaying weather
        /// </summary>
        /// <value>The weather location.</value>
        public string WeatherLocation { get; set; }

        /// <summary>
        /// Gets or sets the weather unit to use when displaying weather
        /// </summary>
        /// <value>The weather unit.</value>
        public WeatherUnits WeatherUnit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP level logging].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP level logging]; otherwise, <c>false</c>.</value>
        public bool EnableHttpLevelLogging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable u pn p].
        /// </summary>
        /// <value><c>true</c> if [enable u pn p]; otherwise, <c>false</c>.</value>
        public bool EnableUPnP { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the legacy web socket port number.
        /// </summary>
        /// <value>The legacy web socket port number.</value>
        public int LegacyWebSocketPortNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable internet providers].
        /// </summary>
        /// <value><c>true</c> if [enable internet providers]; otherwise, <c>false</c>.</value>
        public bool EnableInternetProviders { get; set; }

        /// <summary>
        /// Gets or sets the item by name path.
        /// </summary>
        /// <value>The item by name path.</value>
        public string ItemsByNamePath { get; set; }

        /// <summary>
        /// Gets or sets the metadata path.
        /// </summary>
        /// <value>The metadata path.</value>
        public string MetadataPath { get; set; }

        /// <summary>
        /// Gets or sets the display name of the season zero.
        /// </summary>
        /// <value>The display name of the season zero.</value>
        public string SeasonZeroDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the metadata refresh days.
        /// </summary>
        /// <value>The metadata refresh days.</value>
        public int MetadataRefreshDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        public bool SaveLocalMeta { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh item images].
        /// </summary>
        /// <value><c>true</c> if [refresh item images]; otherwise, <c>false</c>.</value>
        public bool RefreshItemImages { get; set; }

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
        /// Show an output log window for debugging
        /// </summary>
        /// <value><c>true</c> if [show log window]; otherwise, <c>false</c>.</value>
        public bool ShowLogWindow { get; set; }

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
        public int RealtimeMonitorDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable dashboard response caching].
        /// Allows potential contributors without visual studio to modify production dashboard code and test changes.
        /// </summary>
        /// <value><c>true</c> if [enable dashboard response caching]; otherwise, <c>false</c>.</value>
        public bool EnableDashboardResponseCaching { get; set; }

        /// <summary>
        /// Allows the dashboard to be served from a custom path.
        /// </summary>
        /// <value>The dashboard source path.</value>
        public string DashboardSourcePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable tv db updates].
        /// </summary>
        /// <value><c>true</c> if [enable tv db updates]; otherwise, <c>false</c>.</value>
        public bool EnableTvDbUpdates { get; set; }
        public bool EnableTmdbUpdates { get; set; }
        public bool EnableFanArtUpdates { get; set; }

        public bool RequireMobileManualLogin { get; set; }
        public bool RequireNonMobileManualLogin { get; set; }

        /// <summary>
        /// Gets or sets the image saving convention.
        /// </summary>
        /// <value>The image saving convention.</value>
        public ImageSavingConvention ImageSavingConvention { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable people prefix sub folders].
        /// </summary>
        /// <value><c>true</c> if [enable people prefix sub folders]; otherwise, <c>false</c>.</value>
        public bool EnablePeoplePrefixSubFolders { get; set; }

        /// <summary>
        /// Gets or sets the encoding quality.
        /// </summary>
        /// <value>The encoding quality.</value>
        public EncodingQuality MediaEncodingQuality { get; set; }

        public bool AllowVideoUpscaling { get; set; }

        public MetadataOptions[] MetadataOptions { get; set; }

        public bool EnableDebugEncodingLogging { get; set; }
        public string TranscodingTempPath { get; set; }

        public bool EnableAutomaticRestart { get; set; }

        public TvFileOrganizationOptions TvFileOrganizationOptions { get; set; }
        public LiveTvOptions LiveTvOptions { get; set; }

        public bool EnableRealtimeMonitor { get; set; }
        public PathSubstitution[] PathSubstitutions { get; set; }

        public string ServerName { get; set; }
        public string WanDdns { get; set; }

        public string UICulture { get; set; }

        [Obsolete]
        public DlnaOptions DlnaOptions { get; set; }

        public double DownMixAudioBoost { get; set; }

        public NotificationOptions NotificationOptions { get; set; }

        public SubtitleOptions SubtitleOptions { get; set; }

        [Obsolete]
        public string[] ManualLoginClients { get; set; }

        public ChannelOptions ChannelOptions { get; set; }

        [Obsolete]
        public ChapterOptions ChapterOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
        /// </summary>
        public ServerConfiguration()
            : base()
        {
            MediaEncodingQuality = EncodingQuality.Auto;
            ImageSavingConvention = ImageSavingConvention.Compatible;
            HttpServerPortNumber = 8096;
            LegacyWebSocketPortNumber = 8945;
            EnableHttpLevelLogging = true;
            EnableDashboardResponseCaching = true;

            EnableAutomaticRestart = true;
            EnablePeoplePrefixSubFolders = true;

            EnableUPnP = true;
            DownMixAudioBoost = 2;

            MinResumePct = 5;
            MaxResumePct = 90;

            // 5 minutes
            MinResumeDurationSeconds = 300;

            RealtimeMonitorDelay = 30;

            EnableInternetProviders = true; //initial installs will need these

            PathSubstitutions = new PathSubstitution[] { };

            MetadataRefreshDays = 30;
            PreferredMetadataLanguage = "en";
            MetadataCountryCode = "US";

            SortReplaceCharacters = new[] { ".", "+", "%" };
            SortRemoveCharacters = new[] { ",", "&", "-", "{", "}", "'" };
            SortRemoveWords = new[] { "the", "a", "an" };

            ManualLoginClients = new string[] { };

            SeasonZeroDisplayName = "Specials";

            EnableRealtimeMonitor = true;

            UICulture = "en-us";

            MetadataOptions = new[]
            {
                new MetadataOptions(1, 1280) {ItemType = "Book"},
                new MetadataOptions(1, 1280) {ItemType = "MusicAlbum"},
                new MetadataOptions(1, 1280) {ItemType = "MusicArtist"},
                new MetadataOptions(0, 1280) {ItemType = "Season"}
            };

            NotificationOptions = new NotificationOptions();

            SubtitleOptions = new SubtitleOptions();

            ChannelOptions = new ChannelOptions();

            LiveTvOptions = new LiveTvOptions();
            TvFileOrganizationOptions = new TvFileOrganizationOptions();
        }
    }
}
