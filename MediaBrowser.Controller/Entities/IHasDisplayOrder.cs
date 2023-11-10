#nullable disable

using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasDisplayOrder.
    /// </summary>
    public interface IHasDisplayOrder
    {
        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>The display order.</value>
        string DisplayOrder { get; set; }
    }
}
