
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class UserConfiguration
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is administrator.
        /// </summary>
        /// <value><c>true</c> if this instance is administrator; otherwise, <c>false</c>.</value>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// Gets or sets the audio language preference.
        /// </summary>
        /// <value>The audio language preference.</value>
        public string AudioLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [play default audio track].
        /// </summary>
        /// <value><c>true</c> if [play default audio track]; otherwise, <c>false</c>.</value>
        public bool PlayDefaultAudioTrack { get; set; }

        /// <summary>
        /// Gets or sets the subtitle language preference.
        /// </summary>
        /// <value>The subtitle language preference.</value>
        public string SubtitleLanguagePreference { get; set; }

        public bool DisplayMissingEpisodes { get; set; }
        public bool DisplayUnairedEpisodes { get; set; }

        public bool EnableLiveTvManagement { get; set; }
        public bool EnableLiveTvAccess { get; set; }

        public bool EnableMediaPlayback { get; set; }
        public bool EnableContentDeletion { get; set; }

        public bool GroupMoviesIntoBoxSets { get; set; }

        public string[] DisplayChannelsWithinViews { get; set; }

        public string[] ExcludeFoldersFromGrouping { get; set; }

        public UnratedItem[] BlockUnratedItems { get; set; }

        public SubtitlePlaybackMode SubtitleMode { get; set; }
        public bool DisplayCollectionsView { get; set; }
        public bool DisplayFoldersView { get; set; }

        public bool EnableLocalPassword { get; set; }

        public string[] OrderedViews { get; set; }

        public bool IncludeTrailersInSuggestions { get; set; }

        public bool EnableCinemaMode { get; set; }

        public string[] LatestItemsExcludes { get; set; }

        public bool HasMigratedToPolicy { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfiguration" /> class.
        /// </summary>
        public UserConfiguration()
        {
            PlayDefaultAudioTrack = true;
            EnableLiveTvManagement = true;
            EnableMediaPlayback = true;
            EnableLiveTvAccess = true;

            LatestItemsExcludes = new string[] { };
            OrderedViews = new string[] { };
            DisplayChannelsWithinViews = new string[] { };
            BlockUnratedItems = new UnratedItem[] { };

            ExcludeFoldersFromGrouping = new string[] { };
            DisplayCollectionsView = true;

            IncludeTrailersInSuggestions = true;
            EnableCinemaMode = true;
        }
    }
}
