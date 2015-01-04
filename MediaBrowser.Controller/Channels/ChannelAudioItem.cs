using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelAudioItem : Audio, IChannelMediaItem
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }
        public string DataVersion { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public bool IsInfiniteStream { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public string OriginalImageUrl { get; set; }

        public List<ChannelMediaInfo> ChannelMediaSources { get; set; }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.ChannelContent);
        }

        public override string GetUserDataKey()
        {
            return ExternalId;
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

        public ChannelAudioItem()
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

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "channels", ChannelId, Id.ToString("N"));
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
    }
}
