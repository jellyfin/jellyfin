#nullable disable

#pragma warning disable CS1591, SA1306

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.LiveTv
{
    [Common.RequiresSourceSerialisation]
    public class LiveTvProgram : BaseItem, IHasLookupInfo<ItemLookupInfo>, IHasStartDate, IHasProgramAttributes
    {
        private const string EmbyServiceName = "Emby";

        public LiveTvProgram()
        {
            IsVirtualItem = true;
        }

        public string SeriesName { get; set; }

        [JsonIgnore]
        public override SourceType SourceType => SourceType.LiveTV;

        /// <summary>
        /// Gets or sets start date of the program, in UTC.
        /// </summary>
        [JsonIgnore]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is repeat.
        /// </summary>
        /// <value><c>true</c> if this instance is repeat; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        [JsonIgnore]
        public string EpisodeTitle { get; set; }

        [JsonIgnore]
        public string ShowId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>true</c> if this instance is movie; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsMovie { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsSports => Tags.Contains("Sports", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsLive => Tags.Contains("Live", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsNews => Tags.Contains("News", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsKids => Tags.Contains("Kids", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a value indicating whether this instance is premiere.
        /// </summary>
        /// <value><c>true</c> if this instance is premiere; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsPremiere => Tags.Contains("Premiere", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the folder containing the item.
        /// If the item is a folder, it returns the folder itself.
        /// </summary>
        /// <value>The containing folder path.</value>
        [JsonIgnore]
        public override string ContainingFolderPath => Path;

        // [JsonIgnore]
        // public override string MediaType
        // {
        //    get
        //    {
        //        return ChannelType == ChannelType.TV ? Model.Entities.MediaType.Video : Model.Entities.MediaType.Audio;
        //    }
        // }

        [JsonIgnore]
        public bool IsAiring
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= StartDate && now < EndDate;
            }
        }

        [JsonIgnore]
        public bool HasAired
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= EndDate;
            }
        }

        [JsonIgnore]
        public override bool SupportsPeople
        {
            get
            {
                // Optimization
                if (IsNews || IsSports)
                {
                    return false;
                }

                return base.SupportsPeople;
            }
        }

        [JsonIgnore]
        public override bool SupportsAncestors => false;

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (!IsSeries)
            {
                var key = this.GetProviderId(MetadataProvider.Imdb);
                if (!string.IsNullOrEmpty(key))
                {
                    list.Insert(0, key);
                }

                key = this.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(key))
                {
                    list.Insert(0, key);
                }
            }
            else if (!string.IsNullOrEmpty(EpisodeTitle))
            {
                var name = GetClientTypeName();

                list.Insert(0, name + "-" + Name + (EpisodeTitle ?? string.Empty));
            }

            return list;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            var serviceName = ServiceName;

            if (string.Equals(serviceName, EmbyServiceName, StringComparison.OrdinalIgnoreCase) || string.Equals(serviceName, "Next Pvr", StringComparison.OrdinalIgnoreCase))
            {
                return 2.0 / 3;
            }

            return 16.0 / 9;
        }

        public override string GetClientTypeName()
        {
            return "Program";
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.LiveTvProgram;
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N", CultureInfo.InvariantCulture));
        }

        public override bool CanDelete()
        {
            return false;
        }

        private LiveTvOptions GetConfiguration()
        {
            return ConfigurationManager.GetConfiguration<LiveTvOptions>("livetv");
        }

        private ListingsProviderInfo GetListingsProviderInfo()
        {
            if (string.Equals(ServiceName, "Emby", StringComparison.OrdinalIgnoreCase))
            {
                var config = GetConfiguration();

                return config.ListingProviders.FirstOrDefault(i => !string.IsNullOrEmpty(i.MoviePrefix));
            }

            return null;
        }

        protected override string GetNameForMetadataLookup()
        {
            var name = base.GetNameForMetadataLookup();

            var listings = GetListingsProviderInfo();

            if (listings is not null)
            {
                if (!string.IsNullOrEmpty(listings.MoviePrefix) && name.StartsWith(listings.MoviePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(listings.MoviePrefix.Length).Trim();
                }
            }

            return name;
        }
    }
}
