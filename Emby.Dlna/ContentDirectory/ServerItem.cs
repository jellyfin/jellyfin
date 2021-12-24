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
        /// <param name="stubType">The stub type.</param>
        public ServerItem(BaseItem item, StubType? stubType)
        {
            Item = item;

            if (stubType.HasValue)
            {
                StubType = stubType;
            }
            else if (item is IItemByName and not Folder)
            {
                StubType = Dlna.ContentDirectory.StubType.Folder;
            }
        }

        /// <summary>
        /// Gets the underlying base item.
        /// </summary>
        public BaseItem Item { get; }

        /// <summary>
        /// Gets the DLNA item type.
        /// </summary>
        public StubType? StubType { get; }
    }
}
