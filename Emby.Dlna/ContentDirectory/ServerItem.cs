#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;

namespace Emby.Dlna.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ServerItem" />.
    /// </summary>
    internal class ServerItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerItem"/> class.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        public ServerItem(BaseItem item)
        {
            Item = item;

            if (item is IItemByName && !(item is Folder))
            {
                StubType = Dlna.ContentDirectory.StubType.Folder;
            }
        }

        /// <summary>
        /// Gets or sets the underlying base item.
        /// </summary>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the DLNA item type.
        /// </summary>
        public StubType? StubType { get; set; }
    }
}
