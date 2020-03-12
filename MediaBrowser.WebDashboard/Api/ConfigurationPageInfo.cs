#pragma warning disable CS1591

using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.WebDashboard.Api
{
    public class ConfigurationPageInfo
    {
        public ConfigurationPageInfo(IPluginConfigurationPage page)
        {
            Name = page.Name;

            ConfigurationPageType = page.ConfigurationPageType;

            if (page.Plugin != null)
            {
                DisplayName = page.Plugin.Name;
                // Don't use "N" because it needs to match Plugin.Id
                PluginId = page.Plugin.Id.ToString();
            }
        }

        public ConfigurationPageInfo(IPlugin plugin, PluginPageInfo page)
        {
            Name = page.Name;
            EnableInMainMenu = page.EnableInMainMenu;
            MenuSection = page.MenuSection;
            MenuIcon = page.MenuIcon;
            DisplayName = string.IsNullOrWhiteSpace(page.DisplayName) ? plugin.Name : page.DisplayName;

            // Don't use "N" because it needs to match Plugin.Id
            PluginId = plugin.Id.ToString();
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public bool EnableInMainMenu { get; set; }

        public string MenuSection { get; set; }

        public string MenuIcon { get; set; }

        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType { get; set; }

        /// <summary>
        /// Gets or sets the plugin id.
        /// </summary>
        /// <value>The plugin id.</value>
        public string PluginId { get; set; }
    }
}
