using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvChannel : BaseItem, IHasMediaSources
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetClientTypeName() + "-" + Name;
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.LiveTvChannel);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string ExternalId { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType ChannelType { get; set; }

        public string ServiceName { get; set; }

        /// <summary>
        /// Supply the image path if it can be accessed directly from the file system
        /// </summary>
        /// <value>The image path.</value>
        public string ProviderImagePath { get; set; }

        /// <summary>
        /// Supply the image url if it can be downloaded
        /// </summary>
        /// <value>The image URL.</value>
        public string ProviderImageUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has image.
        /// </summary>
        /// <value><c>null</c> if [has image] contains no value, <c>true</c> if [has image]; otherwise, <c>false</c>.</value>
        public bool? HasProviderImage { get; set; }

        protected override string CreateSortName()
        {
            double number = 0;

            if (!string.IsNullOrEmpty(Number))
            {
                double.TryParse(Number, out number);
            }

            return number.ToString("000-") + (Name ?? string.Empty);
        }

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
                Type = MediaSourceType.Default
            };

            list.Add(info);

            return list;
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N"), "metadata");
        }
    }
}
