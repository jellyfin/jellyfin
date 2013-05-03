using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class ItemChangeEventArgs
    /// </summary>
    public class ItemChangeEventArgs
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }
    }
}
