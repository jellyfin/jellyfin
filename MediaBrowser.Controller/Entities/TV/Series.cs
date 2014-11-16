using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series
    /// </summary>
    public class Series : Folder, IHasSoundtracks, IHasTrailers, IHasPreferredMetadataLanguage, IHasDisplayOrder, IHasLookupInfo<SeriesInfo>, IHasSpecialFeatures
    {
        public List<Guid> SpecialFeatureIds { get; set; }
        public List<Guid> SoundtrackIds { get; set; }

        public int SeasonCount { get; set; }

        public int? AnimeSeriesIndex { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }

        public Series()
        {
            AirDays = new List<DayOfWeek>();

            SpecialFeatureIds = new List<Guid>();
            SoundtrackIds = new List<Guid>();
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            DisplaySpecialsWithSeasons = true;
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        public bool DisplaySpecialsWithSeasons { get; set; }

        public List<Guid> LocalTrailerIds { get; set; }
        
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// airdate, dvd or absolute
        /// </summary>
        public string DisplayOrder { get; set; }
        
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SeriesStatus? Status { get; set; }
        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        public List<DayOfWeek> AirDays { get; set; }
        /// <summary>
        /// Gets or sets the air time.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the date last episode added.
        /// </summary>
        /// <value>The date last episode added.</value>
        public DateTime DateLastEpisodeAdded { get; set; }

        /// <summary>
        /// Series aren't included directly in indices - Their Episodes will roll up to them
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IncludeInIndex
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return this.GetProviderId(MetadataProviders.Tvdb) ?? this.GetProviderId(MetadataProviders.Tvcom) ?? base.GetUserDataKey();
        }

        // Studio, Genre and Rating will all be the same so makes no sense to index by these
        protected override IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref")}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref")},
                {LocalizedStrings.Instance.GetString("DirectorDispPref")},
                {LocalizedStrings.Instance.GetString("YearDispPref")},
            };
        }

        [IgnoreDataMember]
        public bool ContainsEpisodesWithoutSeasonFolders
        {
            get
            {
                return Children.OfType<Video>().Any();
            }
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetSeasons(user);
        }

        public IEnumerable<Season> GetSeasons(User user)
        {
            var config = user.Configuration;

            return GetSeasons(user, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Season> GetSeasons(User user, bool includeMissingSeasons, bool includeVirtualUnaired)
        {
            var seasons = base.GetChildren(user, true)
                .OfType<Season>();

            if (!includeMissingSeasons && !includeVirtualUnaired)
            {
                seasons = seasons.Where(i => !i.IsMissingOrVirtualUnaired);
            }
            else
            {
                if (!includeMissingSeasons)
                {
                    seasons = seasons.Where(i => !i.IsMissingSeason);
                }
                if (!includeVirtualUnaired)
                {
                    seasons = seasons.Where(i => !i.IsVirtualUnaired);
                }
            }

            return LibraryManager
                .Sort(seasons, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending)
                .Cast<Season>();
        }

        public IEnumerable<Episode> GetEpisodes(User user, int seasonNumber)
        {
            var config = user.Configuration;

            return GetEpisodes(user, seasonNumber, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes(User user, int seasonNumber, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes)
        {
            return GetEpisodes(user, seasonNumber, includeMissingEpisodes, includeVirtualUnairedEpisodes,
                new List<Episode>());
        }

        internal IEnumerable<Episode> GetEpisodes(User user, int seasonNumber, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes, IEnumerable<Episode> additionalEpisodes)
        {
            var episodes = GetRecursiveChildren(user)
                .OfType<Episode>();

            episodes = FilterEpisodesBySeason(episodes, seasonNumber, DisplaySpecialsWithSeasons);

            episodes = episodes.Concat(additionalEpisodes).Distinct();

            if (!includeMissingEpisodes)
            {
                episodes = episodes.Where(i => !i.IsMissingEpisode);
            }
            if (!includeVirtualUnairedEpisodes)
            {
                episodes = episodes.Where(i => !i.IsVirtualUnaired);
            }

            var sortBy = seasonNumber == 0 ? ItemSortBy.SortName : ItemSortBy.AiredEpisodeOrder;

            return LibraryManager.Sort(episodes, user, new[] { sortBy }, SortOrder.Ascending)
                .Cast<Episode>();
        }

        /// <summary>
        /// Filters the episodes by season.
        /// </summary>
        /// <param name="episodes">The episodes.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="includeSpecials">if set to <c>true</c> [include specials].</param>
        /// <returns>IEnumerable{Episode}.</returns>
        public static IEnumerable<Episode> FilterEpisodesBySeason(IEnumerable<Episode> episodes, int seasonNumber, bool includeSpecials)
        {
            if (!includeSpecials || seasonNumber < 1)
            {
                return episodes.Where(i => (i.PhysicalSeasonNumber ?? -1) == seasonNumber);
            }

            return episodes.Where(i =>
            {
                var episode = i;

                if (episode != null)
                {
                    var currentSeasonNumber = episode.AiredSeasonNumber;

                    return currentSeasonNumber.HasValue && currentSeasonNumber.Value == seasonNumber;
                }

                return false;
            });
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Series);
        }

        public string PreferredMetadataLanguage { get; set; }

        public SeriesInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SeriesInfo>();

            info.AnimeSeriesIndex = AnimeSeriesIndex;

            return info;
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (!ProductionYear.HasValue)
            {
                var info = LibraryManager.ParseName(Name);

                var yearInName = info.Year;

                if (yearInName.HasValue)
                {
                    ProductionYear = yearInName;
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
