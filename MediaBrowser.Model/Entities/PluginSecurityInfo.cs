
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class PluginSecurityInfo
    /// </summary>
    public class PluginSecurityInfo
    {
        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        public bool IsMBSupporter { get; set; }
    }
}
