using MediaBrowser.Controller.Plugins;

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
            PluginId = page.Plugin.Id.ToString("N");
        }
    }
}
