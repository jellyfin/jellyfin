using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelVideoItem : Video, IChannelMediaItem, IHasLookupInfo<ChannelItemLookupInfo>
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }
        public string DataVersion { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }

        public List<ChannelMediaInfo> ChannelMediaSources { get; set; }
        
        public override string GetUserDataKey()
        {
            if (ContentType == ChannelMediaContentType.MovieExtra)
            {
                var key = this.GetProviderId(MetadataProviders.Imdb) ?? this.GetProviderId(MetadataProviders.Tmdb);

                if (!string.IsNullOrWhiteSpace(key))
                {
                    key = key + "-" + ExtraType.ToString().ToLower();

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

        public override bool IsSaveLocalMetadataEnabled()
        {
            return false;
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

            var sources = ChannelManager.GetChannelItemMediaSources(Id.ToString("N"), false, CancellationToken.None)
                    .Result.ToList();

            if (sources.Count > 0)
            {
                return sources;
            }

            list.InsertRange(0, sources);

            return list;
        }

        public ChannelItemLookupInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<ChannelItemLookupInfo>();

            info.ContentType = ContentType;
            info.ExtraType = ExtraType;

            return info;
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "channels", ChannelId, Id.ToString("N"));
        }
    }
}
