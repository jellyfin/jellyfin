using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelVideoItem : Video, IChannelMediaItem
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }
        
        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }

        public override string GetUserDataKey()
        {
            if (ContentType == ChannelMediaContentType.Trailer)
            {
                var key = this.GetProviderId(MetadataProviders.Tmdb) ?? this.GetProviderId(MetadataProviders.Tvdb) ?? this.GetProviderId(MetadataProviders.Imdb) ?? this.GetProviderId(MetadataProviders.Tvcom);

                if (!string.IsNullOrWhiteSpace(key))
                {
                    key = key + "-trailer";

                    // Make sure different trailers have their own data.
                    if (RunTimeTicks.HasValue)
                    {
                        key += "-" + RunTimeTicks.Value.ToString(CultureInfo.InvariantCulture);
                    }

                    return key;
                }
            }

            return base.GetUserDataKey();
        }

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
