using MediaBrowser.Model.Entities;
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
        /// Gets or sets the size of the TMDB fetched poster.
        /// </summary>
        /// <value>The size of the TMDB fetched poster.</value>
        public string TmdbFetchedPosterSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the TMDB fetched profile.
        /// </summary>
        /// <value>The size of the TMDB fetched profile.</value>
        public string TmdbFetchedProfileSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the TMDB fetched backdrop.
        /// </summary>
        /// <value>The size of the TMDB fetched backdrop.</value>
        public string TmdbFetchedBackdropSize { get; set; }

        /// <summary>
        /// Gets or sets the max backdrops.
        /// </summary>
        /// <value>The max backdrops.</value>
        public int MaxBackdrops { get; set; }

        /// <summary>
        /// Options for specific art to download for movies.
        /// </summary>
        public ImageDownloadOptions DownloadMovieImages { get; set; }

        /// <summary>
        /// Options for specific art to download for Series.
        /// </summary>
        public ImageDownloadOptions DownloadSeriesImages { get; set; }

        /// <summary>
        /// Options for specific art to download for Seasons.
        /// </summary>
        public ImageDownloadOptions DownloadSeasonImages { get; set; }

        /// <summary>
        /// Options for specific art to download for MusicArtists.
        /// </summary>
        public ImageDownloadOptions DownloadMusicArtistImages { get; set; }

        /// <summary>
        /// Options for specific art to download for MusicAlbums.
        /// </summary>
        public ImageDownloadOptions DownloadMusicAlbumImages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [download TV season backdrops].
        /// </summary>
        /// <value><c>true</c> if [download TV season banner]; otherwise, <c>false</c>.</value>
        public bool DownloadHDFanArt { get; set; }

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
        /// The list of types that will NOT be allowed to have internet providers run against them even if they are turned on.
        /// </summary>
        /// <value>The internet provider exclude types.</value>
        public string[] InternetProviderExcludeTypes { get; set; }

        /// <summary>
        /// Gets or sets the recent item days.
        /// </summary>
        /// <value>The recent item days.</value>
        public int RecentItemDays { get; set; }

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
        public int FileWatcherDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable developer tools].
        /// </summary>
        /// <value><c>true</c> if [enable developer tools]; otherwise, <c>false</c>.</value>
        public bool EnableDeveloperTools { get; set; }

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

        public ManualLoginCategory[] ManualLoginClients { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable tv db updates].
        /// </summary>
        /// <value><c>true</c> if [enable tv db updates]; otherwise, <c>false</c>.</value>
        public bool EnableTvDbUpdates { get; set; }

        public bool EnableVideoImageExtraction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
        /// </summary>
        public ServerConfiguration()
            : base()
        {
            HttpServerPortNumber = 8096;
            LegacyWebSocketPortNumber = 8945;
            EnableHttpLevelLogging = true;
            EnableDashboardResponseCaching = true;
            EnableVideoImageExtraction = true;

#if (DEBUG)
            EnableDeveloperTools = true;
#endif

            MinResumePct = 5;
            MaxResumePct = 90;
            MinResumeDurationSeconds = Convert.ToInt32(TimeSpan.FromMinutes(5).TotalSeconds);

            FileWatcherDelay = 8;

            RecentItemDays = 10;

            EnableInternetProviders = true; //initial installs will need these
            InternetProviderExcludeTypes = new string[] { };

            ManualLoginClients = new ManualLoginCategory[] { };

            MetadataRefreshDays = 30;
            PreferredMetadataLanguage = "en";
            MetadataCountryCode = "US";
            TmdbFetchedProfileSize = "original"; //w185 w45 h632 or original
            TmdbFetchedPosterSize = "original"; //w500, w342, w185 or original
            TmdbFetchedBackdropSize = "original"; //w1280, w780 or original
            DownloadMovieImages = new ImageDownloadOptions();
            DownloadSeriesImages = new ImageDownloadOptions();
            DownloadSeasonImages = new ImageDownloadOptions();
            DownloadMusicArtistImages = new ImageDownloadOptions();
            DownloadMusicAlbumImages = new ImageDownloadOptions();
            DownloadHDFanArt = true;
            MaxBackdrops = 3;

            SortReplaceCharacters = new[] { ".", "+", "%" };
            SortRemoveCharacters = new[] { ",", "&", "-", "{", "}", "'" };
            SortRemoveWords = new[] { "the", "a", "an" };

            SeasonZeroDisplayName = "Specials";
        }
    }
}
