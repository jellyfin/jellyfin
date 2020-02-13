#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Sync
{
    public class SyncTarget
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
    }
}
