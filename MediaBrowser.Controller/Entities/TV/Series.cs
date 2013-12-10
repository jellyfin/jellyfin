using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series
    /// </summary>
    public class Series : Folder, IHasSoundtracks, IHasTrailers, IHasTags
    {
        public List<Guid> SpecialFeatureIds { get; set; }
        public List<Guid> SoundtrackIds { get; set; }

        public int SeasonCount { get; set; }

        public Series()
        {
            AirDays = new List<DayOfWeek>();

            SpecialFeatureIds = new List<Guid>();
            SoundtrackIds = new List<Guid>();
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            Tags = new List<string>();
            DisplaySpecialsWithSeasons = true;
        }

        public bool DisplaySpecialsWithSeasons { get; set; }

        public List<Guid> LocalTrailerIds { get; set; }
        
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }
     
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
        protected override Dictionary<string, Func<User, IEnumerable<BaseItem>>> GetIndexByOptions()
        {
            return new Dictionary<string, Func<User, IEnumerable<BaseItem>>> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref"), null}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref"), GetIndexByPerformer},
                {LocalizedStrings.Instance.GetString("DirectorDispPref"), GetIndexByDirector},
                {LocalizedStrings.Instance.GetString("YearDispPref"), GetIndexByYear},
            };
        }

        /// <summary>
        /// Creates ResolveArgs on demand
        /// </summary>
        /// <param name="pathInfo">The path info.</param>
        /// <returns>ItemResolveArgs.</returns>
        protected internal override ItemResolveArgs CreateResolveArgs(FileSystemInfo pathInfo = null)
        {
            var args = base.CreateResolveArgs(pathInfo);

            Season.AddMetadataFiles(args);

            return args;
        }

        [IgnoreDataMember]
        public bool ContainsEpisodesWithoutSeasonFolders
        {
            get
            {
                return Children.OfType<Video>().Any();
            }
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren, string indexBy = null)
        {
            return GetSeasons(user);
        }

        public IEnumerable<Season> GetSeasons(User user)
        {
            var seasons = base.GetChildren(user, true)
                .OfType<Season>();

            var config = user.Configuration;

            if (!config.DisplayMissingEpisodes && !config.DisplayUnairedEpisodes)
            {
                seasons = seasons.Where(i => !i.IsMissingOrVirtualUnaired);
            }
            else
            {
                if (!config.DisplayMissingEpisodes)
                {
                    seasons = seasons.Where(i => !i.IsMissingSeason);
                }
                if (!config.DisplayUnairedEpisodes)
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
            var episodes = GetRecursiveChildren(user)
                .OfType<Episode>();

            episodes = FilterEpisodesBySeason(episodes, seasonNumber, DisplaySpecialsWithSeasons);

            var config = user.Configuration;

            if (!config.DisplayMissingEpisodes)
            {
                episodes = episodes.Where(i => !i.IsMissingEpisode);
            }
            if (!config.DisplayUnairedEpisodes)
            {
                episodes = episodes.Where(i => !i.IsVirtualUnaired);
            }

            return LibraryManager.Sort(episodes, user, new[] { ItemSortBy.AiredEpisodeOrder }, SortOrder.Ascending)
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
    }
}
