#pragma warning disable CS1591
#pragma warning disable SA1600

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
}
