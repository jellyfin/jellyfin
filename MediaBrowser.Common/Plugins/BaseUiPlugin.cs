using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Represents a common base class for any plugin that has ui components
    /// </summary>
    public abstract class BaseUiPlugin<TConfigurationType> : BasePlugin<TConfigurationType>, IUIPlugin
        where TConfigurationType : BasePluginConfiguration
    {
        /// <summary>
        /// Returns true or false indicating if the plugin should be downloaded and run within the Ui.
        /// </summary>
        /// <value><c>true</c> if [download to UI]; otherwise, <c>false</c>.</value>
        public sealed override bool DownloadToUi
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        public abstract Version MinimumRequiredUIVersion { get; }
    }
}
