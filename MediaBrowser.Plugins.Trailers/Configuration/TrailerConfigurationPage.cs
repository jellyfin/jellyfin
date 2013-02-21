using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class TrailerConfigurationPage
    /// </summary>
    [Export(typeof(BaseConfigurationPage))]
    class TrailerConfigurationPage : BaseConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Trailers"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public override Stream GetHtmlStream()
        {
            return GetHtmlStreamFromManifestResource("MediaBrowser.Plugins.Trailers.Configuration.configPage.html");
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
