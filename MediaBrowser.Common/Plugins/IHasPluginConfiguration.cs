using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Defines the <see cref="IHasPluginConfiguration" />.
    /// </summary>
    public interface IHasPluginConfiguration
    {
        /// <summary>
        /// Gets the type of configuration this plugin uses.
        /// </summary>
        Type ConfigurationType { get; }

        /// <summary>
        /// Gets the plugin's configuration.
        /// </summary>
        BasePluginConfiguration Configuration { get; }

        /// <summary>
        /// Completely overwrites the current configuration with a new copy.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void UpdateConfiguration(BasePluginConfiguration configuration);
    }
}
