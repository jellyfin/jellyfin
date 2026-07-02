#pragma warning disable CA1819 // Properties should not return arrays

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Plugin configuration class for TMDb library.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets a value to use as the API key for accessing TMDb. This is intentionally excluded from the
        /// settings page as the API key should not need to be changed by most users.
        /// </summary>
        public string TmdbApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether include adult content when searching with TMDb.
        /// </summary>
        public bool IncludeAdult { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tags should be imported for series from TMDb.
        /// </summary>
        public bool ExcludeTagsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tags should be imported for movies from TMDb.
        /// </summary>
        public bool ExcludeTagsMovies { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether season name should be imported from TMDb.
        /// </summary>
        public bool ImportSeasonName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unaired (upcoming) episodes should be created as
        /// virtual items from the episode list provided by TMDb. These populate the "Upcoming" view.
        /// Enabling this will increase scan times.
        /// </summary>
        public bool ImportUnairedEpisodes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether already aired episodes that are not present in the
        /// library should be created as virtual items from the episode list provided by TMDb. These
        /// surface as missing episodes. Enabling this will increase scan times.
        /// </summary>
        public bool ImportMissingEpisodes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether specials (season 0) should be included when creating
        /// virtual unaired or missing episodes. When disabled, specials are never added and any existing
        /// virtual specials created by this provider are removed.
        /// </summary>
        public bool ImportSpecials { get; set; }

        /// <summary>
        /// Gets or sets the ids (the "N" formatted GUIDs from <c>VirtualFolderInfo.ItemId</c>) of the
        /// libraries for which the unaired/missing episode provider is disabled. Whether episodes are
        /// imported at all, and how, is still controlled by the global toggles above; this list only
        /// opts individual libraries out. Libraries not listed here are enabled, so the global toggles
        /// apply to every library unless it is explicitly opted out.
        /// </summary>
        public string[] DisabledMissingEpisodeLibraries { get; set; } = [];

        /// <summary>
        /// Gets or sets how often, in days, the scheduled task re-checks TMDb for newly announced
        /// unaired or missing episodes. This is what keeps the "Upcoming" view current for series
        /// whose local files have not changed.
        /// </summary>
        public int MissingEpisodeRefreshIntervalDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets the number of days a virtual episode is retained after it airs before it is
        /// pruned (when missing episode import is disabled). This grace period leaves recently aired
        /// episodes in place to allow for the delay between an episode airing and its file being added
        /// to the library.
        /// </summary>
        public int UpcomingEpisodeGracePeriodDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets a value indicating the maximum number of cast members to fetch for an item.
        /// </summary>
        public int MaxCastMembers { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating the maximum number of crew members to fetch for an item.
        /// </summary>
        public int MaxCrewMembers { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether to hide cast members without profile images.
        /// </summary>
        public bool HideMissingCastMembers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide crew members without profile images.
        /// </summary>
        public bool HideMissingCrewMembers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the poster image size to fetch.
        /// </summary>
        public string? PosterSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the backdrop image size to fetch.
        /// </summary>
        public string? BackdropSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the logo image size to fetch.
        /// </summary>
        public string? LogoSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the profile image size to fetch.
        /// </summary>
        public string? ProfileSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the still image size to fetch.
        /// </summary>
        public string? StillSize { get; set; }

        /// <summary>
        /// Gets or sets the cache duration in days for similar item results. A value of 0 disables caching.
        /// </summary>
        public int SimilarItemsCacheDays { get; set; } = 7;
    }
}
