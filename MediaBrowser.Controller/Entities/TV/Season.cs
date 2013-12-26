using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Configuration;
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
    /// Class Season
    /// </summary>
    public class Season : Folder
    {

        /// <summary>
        /// Seasons are just containers
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
        /// We want to group into our Series
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return Series;
            }
        }

        // Genre, Rating and Stuido will all be the same
        protected override IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref")}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref")},
                {LocalizedStrings.Instance.GetString("DirectorDispPref")},
                {LocalizedStrings.Instance.GetString("YearDispPref")},
            };
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            if (Series != null)
            {
                var seasonNo = IndexNumber ?? 0;
                return Series.GetUserDataKey() + seasonNo.ToString("000");
            }

            return base.GetUserDataKey();
        }

        /// <summary>
        /// The _series
        /// </summary>
        private Series _series;
        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [IgnoreDataMember]
        public Series Series
        {
            get { return _series ?? (_series = FindParent<Series>()); }
        }

        [IgnoreDataMember]
        public string SeriesPath
        {
            get
            {
                var series = Series;

                if (series != null)
                {
                    return series.Path;
                }

                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get { return Series != null ? Series.OfficialRatingForComparison : base.OfficialRatingForComparison; }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string CustomRatingForComparison
        {
            get { return Series != null ? Series.CustomRatingForComparison : base.CustomRatingForComparison; }
        }

        /// <summary>
        /// Add files from the metadata folder to ResolveArgs
        /// </summary>
        /// <param name="args">The args.</param>
        public static void AddMetadataFiles(ItemResolveArgs args)
        {
            var folder = args.GetFileSystemEntryByName("metadata");

            if (folder != null)
            {
                args.AddMetadataFiles(new DirectoryInfo(folder.FullName).EnumerateFiles());
            }
        }

        /// <summary>
        /// Creates ResolveArgs on demand
        /// </summary>
        /// <param name="pathInfo">The path info.</param>
        /// <returns>ItemResolveArgs.</returns>
        protected internal override ItemResolveArgs CreateResolveArgs(FileSystemInfo pathInfo = null)
        {
            var args = base.CreateResolveArgs(pathInfo);

            AddMetadataFiles(args);

            return args;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber != null ? IndexNumber.Value.ToString("0000") : Name;
        }

        private IEnumerable<Episode> GetEpisodes()
        {
            var series = Series;

            if (series != null && series.ContainsEpisodesWithoutSeasonFolders)
            {
                var seasonNumber = IndexNumber;

                if (seasonNumber.HasValue)
                {
                    return series.RecursiveChildren.OfType<Episode>()
                        .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == seasonNumber.Value);
                }
            }

            return Children.OfType<Episode>();
        }

        [IgnoreDataMember]
        public bool IsMissingSeason
        {
            get { return LocationType == LocationType.Virtual && GetEpisodes().All(i => i.IsMissingEpisode); }
        }

        [IgnoreDataMember]
        public bool IsUnaired
        {
            get { return GetEpisodes().All(i => i.IsUnaired); }
        }

        [IgnoreDataMember]
        public bool IsVirtualUnaired
        {
            get { return LocationType == LocationType.Virtual && IsUnaired; }
        }

        [IgnoreDataMember]
        public bool IsMissingOrVirtualUnaired
        {
            get { return LocationType == LocationType.Virtual && GetEpisodes().All(i => i.IsVirtualUnaired || i.IsMissingEpisode); }
        }

        [IgnoreDataMember]
        public bool IsSpecialSeason
        {
            get { return (IndexNumber ?? -1) == 0; }
        }

        /// <summary>
        /// Gets the episodes.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Episode}.</returns>
        public IEnumerable<Episode> GetEpisodes(User user)
        {
            var config = user.Configuration;

            return GetEpisodes(user, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes(User user, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes)
        {
            if (IndexNumber.HasValue)
            {
                var series = Series;

                if (series != null)
                {
                    return series.GetEpisodes(user, IndexNumber.Value, includeMissingEpisodes, includeVirtualUnairedEpisodes);
                }
            }

            var episodes = GetRecursiveChildren(user)
                .OfType<Episode>();

            if (!includeMissingEpisodes)
            {
                episodes = episodes.Where(i => !i.IsMissingEpisode);
            }
            if (!includeVirtualUnairedEpisodes)
            {
                episodes = episodes.Where(i => !i.IsVirtualUnaired);
            }
            
            return LibraryManager
                .Sort(episodes, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending)
                .Cast<Episode>();
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetEpisodes(user);
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            // Don't block. Let either the entire series rating or episode rating determine it
            return false;
        }
    }
}
