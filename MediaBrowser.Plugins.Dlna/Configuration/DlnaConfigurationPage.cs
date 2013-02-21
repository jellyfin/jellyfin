using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Plugins.Dlna.Configuration
{
    /// <summary>
    /// Class DlnaConfigurationPage
    /// </summary>
    [Export(typeof(BaseConfigurationPage))]
    class DlnaConfigurationPage : BaseConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Dlna"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public override Stream GetHtmlStream()
        {
            return GetHtmlStreamFromManifestResource("MediaBrowser.Plugins.Dlna.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the owner plugin.
        /// </summary>
        /// <returns>BasePlugin.</returns>
        public override IPlugin GetOwnerPlugin()
        {
            return Plugin.Instance;
        }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public override ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }
    }
}
