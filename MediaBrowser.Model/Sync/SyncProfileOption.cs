
namespace MediaBrowser.Model.Sync
{
    public class SyncProfileOption
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
        /// Gets or sets a value indicating whether [enable quality options].
        /// </summary>
        /// <value><c>true</c> if [enable quality options]; otherwise, <c>false</c>.</value>
        public bool EnableQualityOptions { get; set; }

        public SyncProfileOption()
        {
            EnableQualityOptions = true;
        }
    }
}
