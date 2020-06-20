#nullable disable

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class GroupUpdate.
    /// </summary>
    public class GroupUpdate<T>
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the update type.
        /// </summary>
        /// <value>The update type.</value>
        public GroupUpdateType Type { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; set; }
    }
}
