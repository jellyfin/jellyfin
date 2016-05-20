using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series
    /// </summary>
    public class Series : Folder, IHasTrailers, IHasDisplayOrder, IHasLookupInfo<SeriesInfo>, IHasSpecialFeatures, IMetadataContainer, IHasOriginalTitle
    {
        public List<Guid> SpecialFeatureIds { get; set; }

        public int? AnimeSeriesIndex { get; set; }

        public Series()
        {
            AirDays = new List<DayOfWeek>();

            SpecialFeatureIds = new List<Guid>();
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            RemoteTrailerIds = new List<Guid>();
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

        [IgnoreDataMember]
        public override bool SupportsDateLastMediaAdded
        {
            get
            {
                return true;
            }
        }

        public bool DisplaySpecialsWithSeasons { get; set; }

        public List<Guid> LocalTrailerIds { get; set; }
        public List<Guid> RemoteTrailerIds { get; set; }

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
        [IgnoreDataMember]
        public DateTime DateLastEpisodeAdded
        {
            get
            {
                return GetRecursiveChildren(i => i is Episode)
                        .Select(i => i.DateCreated)
                        .OrderByDescending(i => i)
                        .FirstOrDefault();
            }
        }

        [IgnoreDataMember]
        public override string PresentationUniqueKey
        {
            get
            {
                if (EnablePooling())
                {
                    return GetUserDataKeys().First();
                }
                return base.PresentationUniqueKey;
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var key = this.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrWhiteSpace(key))
            {
                list.Insert(0, key);
            }

            key = this.GetProviderId(MetadataProviders.Tvdb);
            if (!string.IsNullOrWhiteSpace(key))
            {
                list.Insert(0, key);
            }

            return list;
        }

        /// <summary>
        /// Gets the trailer ids.
        /// </summary>
        /// <returns>List&lt;Guid&gt;.</returns>
        public List<Guid> GetTrailerIds()
        {
            var list = LocalTrailerIds.ToList();
            list.AddRange(RemoteTrailerIds);
            return list;
        }

        // Studio, Genre and Rating will all be the same so makes no sense to index by these
        protected override IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {
                {"None"},
                {"Performer"},
                {"Director"},
                {"Year"},
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

        protected override Task<QueryResult<BaseItem>> GetItemsInternal(InternalItemsQuery query)
        {
            var user = query.User;

            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

            IEnumerable<BaseItem> items;

            if (query.User == null)
            {
                items = query.Recursive
                   ? GetRecursiveChildren(filter)
                   : Children.Where(filter);
            }
            else
            {
                items = query.Recursive
                   ? GetSeasons(user).Cast<BaseItem>().Concat(GetEpisodes(user)).Where(filter)
                   : GetSeasons(user).Where(filter);
            }

            var result = PostFilterAndSort(items, query);

            return Task.FromResult(result);
        }

        public IEnumerable<Season> GetSeasons(User user, bool includeMissingSeasons, bool includeVirtualUnaired)
        {
            IEnumerable<Season> seasons;

            if (EnablePooling())
            {
                var seriesIds = LibraryManager.GetItemIds(new InternalItemsQuery(user)
                {
                    PresentationUniqueKey = PresentationUniqueKey,
                    IncludeItemTypes = new[] { typeof(Series).Name }
                });

                if (seriesIds.Count > 1)
                {
                    seasons = LibraryManager.GetItemList(new InternalItemsQuery(user)
                    {
                        AncestorIds = seriesIds.Select(i => i.ToString("N")).ToArray(),
                        IncludeItemTypes = new[] { typeof(Season).Name },
                        SortBy = new[] { ItemSortBy.SortName }

                    }).Cast<Season>();
                }
                else
                {
                    seasons = LibraryManager.Sort(base.GetChildren(user, true), user, new[] { ItemSortBy.SortName }, SortOrder.Ascending).OfType<Season>();
                }
            }
            else
            {
                seasons = LibraryManager.Sort(base.GetChildren(user, true), user, new[] { ItemSortBy.SortName }, SortOrder.Ascending).OfType<Season>();
            }

            if (!includeMissingSeasons)
            {
                seasons = seasons.Where(i => !(i.IsMissingSeason));
            }
            if (!includeVirtualUnaired)
            {
                seasons = seasons.Where(i => !i.IsVirtualUnaired);
            }

            return seasons;
        }

        public IEnumerable<Episode> GetEpisodes(User user)
        {
            var config = user.Configuration;

            return GetEpisodes(user, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes(User user, bool includeMissing, bool includeVirtualUnaired)
        {
            var allEpisodes = GetSeasons(user, true, true)
                .SelectMany(i => i.GetEpisodes(user, includeMissing, includeVirtualUnaired))
                .Reverse()
                .ToList();

            // Specials could appear twice based on above - once in season 0, once in the aired season
            // This depends on settings for that series
            // When this happens, remove the duplicate from season 0

            return allEpisodes.DistinctBy(i => i.Id).Reverse();
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Refresh bottom up, children first, then the boxset
            // By then hopefully the  movies within will have Tmdb collection values
            var items = GetRecursiveChildren().ToList();

            var seasons = items.OfType<Season>().ToList();
            var otherItems = items.Except(seasons).ToList();

            var totalItems = seasons.Count + otherItems.Count;
            var numComplete = 0;

            refreshOptions = new MetadataRefreshOptions(refreshOptions);
            refreshOptions.IsPostRecursiveRefresh = true;

            // Refresh current item
            await RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            // Refresh seasons
            foreach (var item in seasons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            // Refresh episodes and other children
            foreach (var item in otherItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var skipItem = false;

                var episode = item as Episode;

                if (episode != null
                    && refreshOptions.MetadataRefreshMode != MetadataRefreshMode.FullRefresh
                    && !refreshOptions.ReplaceAllMetadata
                    && episode.IsMissingEpisode
                    && episode.LocationType == Model.Entities.LocationType.Virtual
                    && episode.PremiereDate.HasValue
                    && (DateTime.UtcNow - episode.PremiereDate.Value).TotalDays > 30)
                {
                    skipItem = true;
                }

                if (!skipItem)
                {
                    await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            await ProviderManager.RefreshSingleItem(this, refreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }

        public IEnumerable<Episode> GetEpisodes(User user, int seasonNumber)
        {
            var config = user.Configuration;

            return GetEpisodes(user, seasonNumber, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        private bool EnablePooling()
        {
            return false;
        }

        public IEnumerable<Episode> GetEpisodes(User user, int seasonNumber, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes)
        {
            IEnumerable<Episode> episodes;

            if (EnablePooling())
            {
                var seriesIds = LibraryManager.GetItemIds(new InternalItemsQuery(user)
                {
                    PresentationUniqueKey = PresentationUniqueKey,
                    IncludeItemTypes = new[] { typeof(Series).Name }
                });

                if (seriesIds.Count > 1)
                {
                    episodes = LibraryManager.GetItemList(new InternalItemsQuery(user)
                    {
                        AncestorIds = seriesIds.Select(i => i.ToString("N")).ToArray(),
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        SortBy = new[] { ItemSortBy.SortName }

                    }).Cast<Episode>();
                }
                else
                {
                    episodes = GetRecursiveChildren(user, new InternalItemsQuery(user)
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name }
                    }).Cast<Episode>();
                }
            }
            else
            {
                episodes = GetRecursiveChildren(user, new InternalItemsQuery(user)
                {
                    IncludeItemTypes = new[] { typeof(Episode).Name }
                }).Cast<Episode>();
            }

            episodes = FilterEpisodesBySeason(episodes, seasonNumber, DisplaySpecialsWithSeasons);

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
                return episodes.Where(i => (i.ParentIndexNumber ?? -1) == seasonNumber);
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

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Series);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

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
