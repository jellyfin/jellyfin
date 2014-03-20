using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelCategoryItem : Folder, IChannelItem
    {
        public string ExternalId { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public string OriginalImageUrl { get; set; }
    }
}
