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

        /// <summary>
        /// Sets the startup directory creation function.
        /// </summary>
        /// <param name="directoryCreateFn">The directory function used to create the configuration folder.</param>
        void SetStartupInfo(Action<string> directoryCreateFn);
    }
}
