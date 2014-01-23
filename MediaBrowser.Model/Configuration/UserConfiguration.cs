
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class UserConfiguration
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Gets or sets the max parental rating.
        /// </summary>
        /// <value>The max parental rating.</value>
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether items with no rating information should be blocked.
        /// </summary>
        /// <value><c>true</c> if items with no rating info should be blocked; otherwise, <c>false</c>.</value>
        public bool BlockNotRated { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether [use custom library].
        /// </summary>
        /// <value><c>true</c> if [use custom library]; otherwise, <c>false</c>.</value>
        public bool UseCustomLibrary { get; set; }

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
        /// Gets or sets the subtitle language preference.
        /// </summary>
        /// <value>The subtitle language preference.</value>
        public string SubtitleLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use forced subtitles only].
        /// </summary>
        /// <value><c>true</c> if [use forced subtitles only]; otherwise, <c>false</c>.</value>
        public bool UseForcedSubtitlesOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden { get; set; }

        public bool IsDisabled { get; set; }

        public bool DisplayMissingEpisodes { get; set; }
        public bool DisplayUnairedEpisodes { get; set; }
        public bool EnableRemoteControlOfOtherUsers { get; set; }

        public bool BlockUnratedMovies { get; set; }
        public bool BlockUnratedTrailers { get; set; }
        public bool BlockUnratedSeries { get; set; }
        public bool BlockUnratedMusic { get; set; }
        public bool BlockUnratedGames { get; set; }
        public bool BlockUnratedBooks { get; set; }

        public bool EnableLiveTvManagement { get; set; }
        public bool EnableLiveTvAccess { get; set; }

        public bool EnableMediaPlayback { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfiguration" /> class.
        /// </summary>
        public UserConfiguration()
        {
            IsAdministrator = true;
            EnableRemoteControlOfOtherUsers = true;
            BlockNotRated = false;

            EnableLiveTvManagement = true;
            EnableMediaPlayback = true;
            EnableLiveTvAccess = true;
        }
    }
}
