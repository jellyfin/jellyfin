using MediaBrowser.Common.Plugins;
using System.IO;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Class BaseConfigurationPage
    /// </summary>
    public abstract class BaseConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public virtual string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public virtual ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        /// <summary>
        /// Gets the HTML stream from manifest resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>Stream.</returns>
        protected Stream GetHtmlStreamFromManifestResource(string resource)
        {
            return GetType().Assembly.GetManifestResourceStream(resource);
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public abstract Stream GetHtmlStream();

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value>The name of the plugin.</value>
        public virtual string OwnerPluginName
        {
            get { return GetOwnerPlugin().Name; }
        }
        
        /// <summary>
        /// Gets the owner plugin.
        /// </summary>
        /// <returns>BasePlugin.</returns>
        public abstract IPlugin GetOwnerPlugin();
    }

    /// <summary>
    /// Enum ConfigurationPageType
    /// </summary>
    public enum ConfigurationPageType
    {
        /// <summary>
        /// The plugin configuration
        /// </summary>
        PluginConfiguration,
        /// <summary>
        /// The none
        /// </summary>
        None
    }
}
