#nullable disable

using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Sorting
{
    /// <summary>
    /// Represents a BaseItem comparer that requires a item list manager to perform its comparison.
    /// </summary>
    public interface IItemListBaseItemComparer : IUserBaseItemComparer
    {
        /// <summary>
        /// Gets or sets the item list manager.
        /// </summary>
        /// <value>The item list manager.</value>
        IItemListManager ItemListManager { get; set; }
    }
}
