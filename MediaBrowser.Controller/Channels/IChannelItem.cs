using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelItem : IHasImages
    {
        string ExternalId { get; set; }

        ChannelItemType ChannelItemType { get; set; }

        bool IsInfiniteStream { get; set; }

        ChannelMediaContentType ContentType { get; set; }

        string OriginalImageUrl { get; set; }
    }
}
