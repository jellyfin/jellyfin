using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Updates;
using MediaBrowser.Model.Weather;
using ProtoBuf;
using System;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents the server configuration.
    /// </summary>
    [ProtoContract]
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable HTTP level logging].
        /// </summary>
        /// <value><c>true</c> if [enable HTTP level logging]; otherwise, <c>false</c>.</value>
        [ProtoMember(56)]
        public bool EnableHttpLevelLogging { get; set; }

        /// <summary>
        /// Gets or sets the HTTP server port number.
        /// </summary>
        /// <value>The HTTP server port number.</value>
        [ProtoMember(2)]
        public int HttpServerPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the legacy web socket port number.
        /// </summary>
        /// <value>The legacy web socket port number.</value>
        [ProtoMember(59)]
        public int LegacyWebSocketPortNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable internet providers].
        /// </summary>
        /// <value><c>true</c> if [enable internet providers]; otherwise, <c>false</c>.</value>
        [ProtoMember(6)]
        public bool EnableInternetProviders { get; set; }

        /// <summary>
        /// Gets or sets the zip code to use when displaying weather
        /// </summary>
        /// <value>The weather location.</value>
        [ProtoMember(7)]
        public string WeatherLocation { get; set; }

        /// <summary>
        /// Gets or sets the weather unit to use when displaying weather
        /// </summary>
        /// <value>The weather unit.</value>
        [ProtoMember(8)]
        public WeatherUnits WeatherUnit { get; set; }

        /// <summary>
        /// Gets or sets the metadata refresh days.
        /// </summary>
        /// <value>The metadata refresh days.</value>
        [ProtoMember(9)]
        public int MetadataRefreshDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        [ProtoMember(10)]
        public bool SaveLocalMeta { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh item images].
        /// </summary>
        /// <value><c>true</c> if [refresh item images]; otherwise, <c>false</c>.</value>
        [ProtoMember(11)]
        public bool RefreshItemImages { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        [ProtoMember(12)]
        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        [ProtoMember(13)]
        public string MetadataCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the size of the TMDB fetched poster.
        /// </summary>
        /// <value>The size of the TMDB fetched poster.</value>
        [ProtoMember(14)]
        public string TmdbFetchedPosterSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the TMDB fetched profile.
        /// </summary>
        /// <value>The size of the TMDB fetched profile.</value>
        [ProtoMember(15)]
        public string TmdbFetchedProfileSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the TMDB fetched backdrop.
        /// </summary>
        /// <value>The size of the TMDB fetched backdrop.</value>
        [ProtoMember(16)]
        public string TmdbFetchedBackdropSize { get; set; }

        /// <summary>
        /// Gets or sets the max backdrops.
        /// </summary>
        /// <value>The max backdrops.</value>
        [ProtoMember(17)]
        public int MaxBackdrops { get; set; }

        /// <summary>
        /// Options for specific art to download for movies.
        /// </summary>
        [ProtoMember(40)]
        public ImageDownloadOptions DownloadMovieImages { get; set; }

        /// <summary>
        /// Options for specific art to download for Series.
        /// </summary>
        [ProtoMember(41)]
        public ImageDownloadOptions DownloadSeriesImages { get; set; }

        /// <summary>
        /// Options for specific art to download for Seasons.
        /// </summary>
        [ProtoMember(42)]
        public ImageDownloadOptions DownloadSeasonImages { get; set; }

        /// <summary>
        /// Options for specific art to download for MusicArtists.
        /// </summary>
        [ProtoMember(43)]
        public ImageDownloadOptions DownloadMusicArtistImages { get; set; }

        /// <summary>
        /// Options for specific art to download for MusicAlbums.
        /// </summary>
        [ProtoMember(44)]
        public ImageDownloadOptions DownloadMusicAlbumImages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [download TV season backdrops].
        /// </summary>
        /// <value><c>true</c> if [download TV season banner]; otherwise, <c>false</c>.</value>
        [ProtoMember(52)]
        public bool DownloadHDFanArt { get; set; }

        /// <summary>
        /// Gets or sets the name of the item repository that should be used
        /// </summary>
        /// <value>The item repository.</value>
        [ProtoMember(24)]
        public string ItemRepository { get; set; }

        /// <summary>
        /// Gets or sets the name of the user repository that should be used
        /// </summary>
        /// <value>The user repository.</value>
        [ProtoMember(25)]
        public string UserRepository { get; set; }

        /// <summary>
        /// Gets or sets the name of the user data repository that should be used
        /// </summary>
        /// <value>The user data repository.</value>
        [ProtoMember(26)]
        public string UserDataRepository { get; set; }

        /// <summary>
        /// Characters to be replaced with a ' ' in strings to create a sort name
        /// </summary>
        /// <value>The sort replace characters.</value>
        [ProtoMember(27)]
        public string[] SortReplaceCharacters { get; set; }

        /// <summary>
        /// Characters to be removed from strings to create a sort name
        /// </summary>
        /// <value>The sort remove characters.</value>
        [ProtoMember(28)]
        public string[] SortRemoveCharacters { get; set; }

        /// <summary>
        /// Words to be removed from strings to create a sort name
        /// </summary>
        /// <value>The sort remove words.</value>
        [ProtoMember(29)]
        public string[] SortRemoveWords { get; set; }

        /// <summary>
        /// Show an output log window for debugging
        /// </summary>
        /// <value><c>true</c> if [show log window]; otherwise, <c>false</c>.</value>
        [ProtoMember(30)]
        public bool ShowLogWindow { get; set; }

        /// <summary>
        /// Gets or sets the name of the user data repository that should be used
        /// </summary>
        /// <value>The display preferences repository.</value>
        [ProtoMember(31)]
        public string DisplayPreferencesRepository { get; set; }

        /// <summary>
        /// The list of types that will NOT be allowed to have internet providers run against them even if they are turned on.
        /// </summary>
        /// <value>The internet provider exclude types.</value>
        [ProtoMember(32)]
        public string[] InternetProviderExcludeTypes { get; set; }

        /// <summary>
        /// Gets or sets the recent item days.
        /// </summary>
        /// <value>The recent item days.</value>
        [ProtoMember(34)]
        public int RecentItemDays { get; set; }

        /// <summary>
        /// Gets or sets the minimum percentage of an item that must be played in order for playstate to be updated.
        /// </summary>
        /// <value>The min resume PCT.</value>
        [ProtoMember(36)]
        public int MinResumePct { get; set; }

        /// <summary>
        /// Gets or sets the maximum percentage of an item that can be played while still saving playstate. If this percentage is crossed playstate will be reset to the beginning and the item will be marked watched.
        /// </summary>
        /// <value>The max resume PCT.</value>
        [ProtoMember(37)]
        public int MaxResumePct { get; set; }

        /// <summary>
        /// Gets or sets the minimum duration that an item must have in order to be eligible for playstate updates..
        /// </summary>
        /// <value>The min resume duration seconds.</value>
        [ProtoMember(38)]
        public int MinResumeDurationSeconds { get; set; }

        /// <summary>
        /// The delay in seconds that we will wait after a file system change to try and discover what has been added/removed
        /// Some delay is necessary with some items because their creation is not atomic.  It involves the creation of several
        /// different directories and files.
        /// </summary>
        /// <value>The file watcher delay.</value>
        [ProtoMember(55)]
        public int FileWatcherDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable developer tools].
        /// </summary>
        /// <value><c>true</c> if [enable developer tools]; otherwise, <c>false</c>.</value>
        [ProtoMember(57)]
        public bool EnableDeveloperTools { get; set; }

        // Next Proto number ====> 61

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfiguration" /> class.
        /// </summary>
        public ServerConfiguration()
            : base()
        {
            HttpServerPortNumber = 8096;
            LegacyWebSocketPortNumber = 8945;
            EnableHttpLevelLogging = true;

#if (DEBUG)
            EnableDeveloperTools = true;
#endif

            MinResumePct = 5;
            MaxResumePct = 90;
            MinResumeDurationSeconds = Convert.ToInt32(TimeSpan.FromMinutes(5).TotalSeconds);

            FileWatcherDelay = 8;

            RecentItemDays = 14;

            EnableInternetProviders = true; //initial installs will need these
            InternetProviderExcludeTypes = new string[] { };

            MetadataRefreshDays = 30;
            PreferredMetadataLanguage = "en";
            MetadataCountryCode = "US";
            TmdbFetchedProfileSize = "w185"; //w185 w45 h632 or original
            TmdbFetchedPosterSize = "w500"; //w500, w342, w185 or original
            TmdbFetchedBackdropSize = "w1280"; //w1280, w780 or original
            DownloadMovieImages = new ImageDownloadOptions {Backdrops = true, Primary = true};
            DownloadSeriesImages = new ImageDownloadOptions {Backdrops = true, Primary = true, Banner = true, Logo = true};
            DownloadSeasonImages = new ImageDownloadOptions {Backdrops = false, Primary = true, Banner = true};
            DownloadMusicArtistImages = new ImageDownloadOptions {Backdrops = true, Primary = true, Banner = true, Thumb = true};
            DownloadMusicAlbumImages = new ImageDownloadOptions {Backdrops = true, Primary = false};
            DownloadHDFanArt = true;
            MaxBackdrops = 4;

            SortReplaceCharacters = new [] { ".", "+", "%" };
            SortRemoveCharacters = new [] { ",", "&", "-", "{", "}", "'" };
            SortRemoveWords = new [] { "the", "a", "an" };
        }
    }
}
