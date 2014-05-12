using System.Linq;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelAudioItem : Audio, IChannelMediaItem
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }
        
        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.ChannelContent);
        }

        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }
    }
}
