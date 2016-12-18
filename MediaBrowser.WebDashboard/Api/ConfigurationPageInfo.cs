using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.WebDashboard.Api
{
    public class ConfigurationPageInfo
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType { get; set; }

        /// <summary>
        /// Gets or sets the plugin id.
        /// </summary>
        /// <value>The plugin id.</value>
        public string PluginId { get; set; }

        public ConfigurationPageInfo(IPluginConfigurationPage page)
        {
            Name = page.Name;
            ConfigurationPageType = page.ConfigurationPageType;

            // Don't use "N" because it needs to match Plugin.Id
            PluginId = page.Plugin.Id.ToString();
        }

        public ConfigurationPageInfo(IPlugin plugin, PluginPageInfo page)
        {
            Name = page.Name;

            // Don't use "N" because it needs to match Plugin.Id
            PluginId = plugin.Id.ToString();
        }
    }
}
