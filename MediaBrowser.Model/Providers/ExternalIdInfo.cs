
namespace MediaBrowser.Model.Providers
{
    public class ExternalIdInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        public string ItemType { get; set; }
        
        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        public string FormatString { get; set; }
    }

    public class ExternalUrl
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        public string Url { get; set; }
    }
}
