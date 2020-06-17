using System.IO;
using MediaBrowser.Common.Plugins;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface IConfigurationPage.
    /// </summary>
    public interface IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        ConfigurationPageType ConfigurationPageType { get; }

        /// <summary>
        /// Gets the plugin.
        /// </summary>
        /// <value>The plugin.</value>
        IPlugin Plugin { get; }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        Stream GetHtmlStream();
    }

    /// <summary>
    /// Enum ConfigurationPageType.
    /// </summary>
    public enum ConfigurationPageType
    {
        /// <summary>
        /// The plugin configuration.
        /// </summary>
        PluginConfiguration,
        /// <summary>
        /// The none.
        /// </summary>
        None
    }
}
