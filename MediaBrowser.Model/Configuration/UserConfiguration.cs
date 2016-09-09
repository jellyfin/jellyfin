
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class UserConfiguration
    /// </summary>
    public class UserConfiguration
    {
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

        public string[] ExcludeFoldersFromGrouping { get; set; }
        public string[] GroupedFolders { get; set; }

        public SubtitlePlaybackMode SubtitleMode { get; set; }
        public bool DisplayCollectionsView { get; set; }

        public bool EnableLocalPassword { get; set; }

        public string[] OrderedViews { get; set; }

        public string[] LatestItemsExcludes { get; set; }
        public string[] PlainFolderViews { get; set; }

        public bool HidePlayedInLatest { get; set; }

        public bool RememberAudioSelections { get; set; }
        public bool RememberSubtitleSelections { get; set; }
        public bool EnableNextEpisodeAutoPlay { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfiguration" /> class.
        /// </summary>
        public UserConfiguration()
        {
            EnableNextEpisodeAutoPlay = true;
            RememberAudioSelections = true;
            RememberSubtitleSelections = true;

            HidePlayedInLatest = true;
            PlayDefaultAudioTrack = true;

            LatestItemsExcludes = new string[] { };
            OrderedViews = new string[] { };

            PlainFolderViews = new string[] { };

            GroupedFolders = new string[] { };
        }
    }
}
