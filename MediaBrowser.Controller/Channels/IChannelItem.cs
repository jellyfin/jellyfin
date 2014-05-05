using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelItem : IHasImages
    {
        string ChannelId { get; set; }

        string ExternalId { get; set; }

        ChannelItemType ChannelItemType { get; set; }

        string OriginalImageUrl { get; set; }
    }

    public interface IChannelMediaItem : IChannelItem
    {
        bool IsInfiniteStream { get; set; }

        ChannelMediaContentType ContentType { get; set; }
    }
}
