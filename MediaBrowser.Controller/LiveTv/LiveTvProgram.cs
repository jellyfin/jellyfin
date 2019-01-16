using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvProgram : BaseItem, IHasLookupInfo<ItemLookupInfo>, IHasStartDate, IHasProgramAttributes
    {
        public LiveTvProgram()
        {
            IsVirtualItem = true;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (!IsSeries)
            {
                var key = this.GetProviderId(MetadataProviders.Imdb);
                if (!string.IsNullOrEmpty(key))
                {
                    list.Insert(0, key);
                }

                key = this.GetProviderId(MetadataProviders.Tmdb);
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

        private static string EmbyServiceName = "Emby";
        public override double GetDefaultPrimaryImageAspectRatio()
        {
            var serviceName = ServiceName;

            if (string.Equals(serviceName, EmbyServiceName, StringComparison.OrdinalIgnoreCase) || string.Equals(serviceName, "Next Pvr", StringComparison.OrdinalIgnoreCase))
            {
                return 2.0 / 3;
            }
            else
            {
                return 16.0 / 9;
            }
        }

        [IgnoreDataMember]
        public override SourceType SourceType
        {
            get { return SourceType.LiveTV; }
        }

        /// <summary>
        /// The start date of the program, in UTC.
        /// </summary>
        [IgnoreDataMember]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is repeat.
        /// </summary>
        /// <value><c>true</c> if this instance is repeat; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        [IgnoreDataMember]
        public string EpisodeTitle { get; set; }

        [IgnoreDataMember]
        public string ShowId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>true</c> if this instance is movie; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsSports
        {
            get
            {
                return Tags.Contains("Sports", StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsLive
        {
            get
            {
                return Tags.Contains("Live", StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsNews
        {
            get
            {
                return Tags.Contains("News", StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsKids
        {
            get
            {
                return Tags.Contains("Kids", StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is premiere.
        /// </summary>
        /// <value><c>true</c> if this instance is premiere; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsPremiere
        {
            get
            {
                return Tags.Contains("Premiere", StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        [IgnoreDataMember]
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        //[IgnoreDataMember]
        //public override string MediaType
        //{
        //    get
        //    {
        //        return ChannelType == ChannelType.TV ? Model.Entities.MediaType.Video : Model.Entities.MediaType.Audio;
        //    }
        //}

        [IgnoreDataMember]
        public bool IsAiring
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= StartDate && now < EndDate;
            }
        }

        [IgnoreDataMember]
        public bool HasAired
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= EndDate;
            }
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
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N"));
        }

        public override bool CanDelete()
        {
            return false;
        }

        [IgnoreDataMember]
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

        [IgnoreDataMember]
        public override bool SupportsAncestors
        {
            get
            {
                return false;
            }
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

            if (listings != null)
            {
                if (!string.IsNullOrEmpty(listings.MoviePrefix) && name.StartsWith(listings.MoviePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(listings.MoviePrefix.Length).Trim();
                }
            }

            return name;
        }

        public override List<ExternalUrl> GetRelatedUrls()
        {
            var list = base.GetRelatedUrls();

            var imdbId = this.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrEmpty(imdbId))
            {
                if (IsMovie)
                {
                    list.Add(new ExternalUrl
                    {
                        Name = "Trakt",
                        Url = string.Format("https://trakt.tv/movies/{0}", imdbId)
                    });
                }
            }

            return list;
        }

        public string SeriesName { get; set;}
    }
}
