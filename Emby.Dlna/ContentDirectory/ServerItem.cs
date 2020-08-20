#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;

namespace Emby.Dlna.ContentDirectory
{
    internal class ServerItem
    {
        public ServerItem(BaseItem item)
        {
            Item = item;

            if (item is IItemByName && !(item is Folder))
            {
                StubType = Dlna.ContentDirectory.StubType.Folder;
            }
        }

        public BaseItem Item { get; set; }

        public StubType? StubType { get; set; }
    }
}
