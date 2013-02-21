using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using System;
using System.ComponentModel.Composition;

namespace MediaBrowser.Plugins.Tmt5
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    [Export(typeof(IPlugin))]
    public class Plugin : BaseUiPlugin<BasePluginConfiguration>
    {
        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "TMT5 Integration"; }
        }

        /// <summary>
        /// Gets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        public override Version MinimumRequiredUIVersion
        {
            get { return new Version("2.9.4782.23738"); }
        }
    }
}
