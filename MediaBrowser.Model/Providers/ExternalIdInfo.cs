#nullable disable
#pragma warning disable CS1591

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
        /// Gets or sets the URL format string.
        /// </summary>
        /// <value>The URL format string.</value>
        public string UrlFormatString { get; set; }
    }
}
