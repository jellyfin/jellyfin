#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvChannel : BaseItem, IHasMediaSources, IHasProgramAttributes
    {
        [JsonIgnore]
        public override bool SupportsPositionTicksResume => false;

        [JsonIgnore]
        public override SourceType SourceType => SourceType.LiveTV;

        [JsonIgnore]
        public override bool EnableRememberingTrackSelections => false;

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

        [JsonIgnore]
        public override LocationType LocationType => LocationType.Remote;

        [JsonIgnore]
        public override string MediaType => ChannelType == ChannelType.Radio ? Model.Entities.MediaType.Audio : Model.Entities.MediaType.Video;

        [JsonIgnore]
        public bool IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsSports { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsNews { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsKids => Tags.Contains("Kids", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        [JsonIgnore]
        public string EpisodeTitle { get; set; }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (!ConfigurationManager.Configuration.DisableLiveTvChannelUserDataName)
            {
                list.Insert(0, GetClientTypeName() + "-" + Name);
            }

            return list;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.LiveTvChannel;
        }

        protected override string CreateSortName()
        {
            if (!string.IsNullOrEmpty(Number))
            {
                if (double.TryParse(Number, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0:00000.0}", number) + "-" + (Name ?? string.Empty);
                }
            }

            return (Number ?? string.Empty) + "-" + (Name ?? string.Empty);
        }

        public override string GetClientTypeName()
        {
            return "TvChannel";
        }

        public IEnumerable<BaseItem> GetTaggedItems()
        {
            return new List<BaseItem>();
        }

        public override List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            var list = new List<MediaSourceInfo>();

            var info = new MediaSourceInfo
            {
                Id = Id.ToString("N", CultureInfo.InvariantCulture),
                Protocol = PathProtocol ?? MediaProtocol.File,
                MediaStreams = Array.Empty<MediaStream>(),
                Name = Name,
                Path = Path,
                RunTimeTicks = RunTimeTicks,
                Type = MediaSourceType.Placeholder,
                IsInfiniteStream = RunTimeTicks == null
            };

            list.Add(info);

            return list;
        }

        public override List<MediaStream> GetMediaStreams()
        {
            return new List<MediaStream>();
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N", CultureInfo.InvariantCulture), "metadata");
        }

        public override bool CanDelete()
        {
            return false;
        }
    }
}
