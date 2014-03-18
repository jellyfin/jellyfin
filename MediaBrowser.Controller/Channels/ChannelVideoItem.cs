using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelVideoItem : Video, IChannelItem
    {
        public string ExternalId { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }
    }
}
