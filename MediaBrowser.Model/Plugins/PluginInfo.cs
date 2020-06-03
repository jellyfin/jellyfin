#nullable disable
namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration file.
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public string ConfigurationFileName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>The image URL.</value>
        public string ImageUrl { get; set; }
    }
}
