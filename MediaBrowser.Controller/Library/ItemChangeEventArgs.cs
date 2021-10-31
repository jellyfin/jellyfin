#nullable disable

#pragma warning disable CA1711, CS1591

using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class ItemChangeEventArgs.
    /// </summary>
    public class ItemChangeEventArgs
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }

        public BaseItem Parent { get; set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public ItemUpdateType UpdateReason { get; set; }
    }
}
