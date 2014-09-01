using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
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

        public List<ChannelMediaInfo> ChannelMediaSources { get; set; }
        
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

            return ExternalId;
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

        public ChannelVideoItem()
        {
            ChannelMediaSources = new List<ChannelMediaInfo>();
        }

        public override LocationType LocationType
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                {
                    return LocationType.Remote;
                }

                return base.LocationType;
            }
        }

        public override IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            var list = base.GetMediaSources(enablePathSubstitution).ToList();

            list.InsertRange(0, ChannelManager.GetCachedChannelItemMediaSources(Id.ToString("N")));

            return list;
        }
    }
}
