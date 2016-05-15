using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvChannel : BaseItem, IHasMediaSources
    {
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            list.Insert(0, GetClientTypeName() + "-" + Name);
            return list;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.LiveTvChannel;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override SourceType SourceType
        {
            get { return SourceType.LiveTV; }
            set { }
        }

        [IgnoreDataMember]
        public override bool EnableRememberingTrackSelections
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType ChannelType { get; set; }

        [IgnoreDataMember]
        public override LocationType LocationType
        {
            get
            {
                // TODO: This should be removed
                return LocationType.Remote;
            }
        }

        protected override string CreateSortName()
        {
            double number = 0;

            if (!string.IsNullOrEmpty(Number))
            {
                double.TryParse(Number, out number);
            }

            return number.ToString("000-") + (Name ?? string.Empty);
        }

        [IgnoreDataMember]
        public override string MediaType
        {
            get
            {
                return ChannelType == ChannelType.Radio ? Model.Entities.MediaType.Audio : Model.Entities.MediaType.Video;
            }
        }

        public override string GetClientTypeName()
        {
            return "TvChannel";
        }

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            return new List<BaseItem>();
        }

        public IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            var list = new List<MediaSourceInfo>();

            var locationType = LocationType;

            var info = new MediaSourceInfo
            {
                Id = Id.ToString("N"),
                Protocol = locationType == LocationType.Remote ? MediaProtocol.Http : MediaProtocol.File,
                MediaStreams = new List<MediaStream>(),
                Name = Name,
                Path = Path,
                RunTimeTicks = RunTimeTicks,
                Type = MediaSourceType.Placeholder
            };

            list.Add(info);

            return list;
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N"), "metadata");
        }

        public override bool CanDelete()
        {
            return false;
        }
    }
}
