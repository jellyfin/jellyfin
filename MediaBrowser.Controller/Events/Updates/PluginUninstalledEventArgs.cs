using Jellyfin.Data.Events;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Controller.Events.Updates
{
    /// <summary>
    /// An event that occurs when a plugin is uninstalled.
    /// </summary>
    public class PluginUninstalledEventArgs : GenericEventArgs<PluginInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUninstalledEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The plugin.</param>
        public PluginUninstalledEventArgs(PluginInfo arg) : base(arg)
        {
        }
    }
}
