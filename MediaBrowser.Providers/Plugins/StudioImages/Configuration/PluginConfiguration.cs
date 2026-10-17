using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.StudioImages.Configuration
{
    /// <summary>
    /// Plugin configuration class for the studio image provider.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the next scheduled task run should replace
        /// every studio image from the artwork bundle, even if the local bundle is already
        /// up to date. The flag is cleared after the refresh is queued.
        /// </summary>
        public bool ReplaceAllImagesOnNextRun { get; set; }
    }
}
