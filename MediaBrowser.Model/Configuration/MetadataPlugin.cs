using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Model.Configuration
{
    public class MetadataPlugin
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public MetadataPluginType Type { get; set; }
    }

    public class MetadataPluginSummary
    {
        /// <summary>
        /// Gets or sets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        public string ItemType { get; set; }

        /// <summary>
        /// Gets or sets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        public List<MetadataPlugin> Plugins { get; set; }

        /// <summary>
        /// Gets or sets the supported image types.
        /// </summary>
        /// <value>The supported image types.</value>
        public List<ImageType> SupportedImageTypes { get; set; }

        public MetadataPluginSummary()
        {
            SupportedImageTypes = new List<ImageType>();
            Plugins = new List<MetadataPlugin>();
        }
    }

    /// <summary>
    /// Enum MetadataPluginType
    /// </summary>
    public enum MetadataPluginType
    {
        LocalImageProvider,
        ImageFetcher,
        ImageSaver,
        LocalMetadataProvider,
        MetadataFetcher,
        MetadataSaver
    }
}
