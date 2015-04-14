
namespace MediaBrowser.Model.Sync
{
    public class SyncQualityOption
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is default.
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        public bool IsDefault { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is original quality.
        /// </summary>
        /// <value><c>true</c> if this instance is original quality; otherwise, <c>false</c>.</value>
        public bool IsOriginalQuality { get; set; }
    }
}
