using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Plugin configuration class for TMDb library.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
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
        /// Gets or sets a value indicating the maximum number of cast members to fetch for an item.
        /// </summary>
        public int MaxCastMembers { get; set; } = 15;
    }
}
