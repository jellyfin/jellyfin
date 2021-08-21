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
    }
}
