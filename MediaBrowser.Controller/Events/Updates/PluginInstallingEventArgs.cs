using Jellyfin.Data.Events;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Controller.Events.Updates
{
    /// <summary>
    /// An event that occurs when a plugin is installing.
    /// </summary>
    public class PluginInstallingEventArgs : GenericEventArgs<InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallingEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The installation info.</param>
        public PluginInstallingEventArgs(InstallationInfo arg) : base(arg)
        {
        }
    }
}
