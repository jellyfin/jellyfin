using MediaBrowser.Controller.Entities.Audio;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelAudioItem : Audio, IChannelMediaItem
    {
        public string ExternalId { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }
    }
}
