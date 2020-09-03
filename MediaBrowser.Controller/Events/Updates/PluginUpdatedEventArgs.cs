using Jellyfin.Data.Events;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Controller.Events.Updates
{
    /// <summary>
    /// An event that occurs when a plugin is updated.
    /// </summary>
    public class PluginUpdatedEventArgs : GenericEventArgs<InstallationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The installation info.</param>
        public PluginUpdatedEventArgs(InstallationInfo arg) : base(arg)
        {
        }
    }
}
