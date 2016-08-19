using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Controller.Entities.Audio;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder, IHasTrailers, IHasDisplayOrder, IHasLookupInfo<BoxSetInfo>, IHasShares
    {
        public List<Share> Shares { get; set; }

        public BoxSet()
        {
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            RemoteTrailerIds = new List<Guid>();

            DisplayOrder = ItemSortBy.PremiereDate;
            Shares = new List<Share>();
        }

        protected override bool FilterLinkedChildrenPerUser
        {
            get
            {
                return true;
            }
        }

        public List<Guid> LocalTrailerIds { get; set; }
        public List<Guid> RemoteTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>The display order.</value>
        public string DisplayOrder { get; set; }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Movie);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Movie;
        }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            if (IsLegacyBoxSet)
            {
                return base.GetNonCachedChildren(directoryService);
            }
            return new List<BaseItem>();
        }

        protected override IEnumerable<BaseItem> LoadChildren()
        {
            if (IsLegacyBoxSet)
            {
                return base.LoadChildren();
            }

            // Save a trip to the database
            return new List<BaseItem>();
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
        protected override bool SupportsShortcutChildren
        {
            get
            {
                if (IsLegacyBoxSet)
                {
                    return true;
                }

                return false;
            }
        }

        [IgnoreDataMember]
        private bool IsLegacyBoxSet
        {
            get
            {
                return !FileSystem.ContainsSubPath(ConfigurationManager.ApplicationPaths.DataPath, Path);
            }
        }

        public override bool IsAuthorizedToDelete(User user)
        {
            return true;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
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

        /// <summary>
        /// Updates the official rating based on content and returns true or false indicating if it changed.
        /// </summary>
        /// <returns></returns>
        public bool UpdateRatingToContent()
        {
            var currentOfficialRating = OfficialRating;

            // Gather all possible ratings
            var ratings = GetRecursiveChildren()
                .Concat(GetLinkedChildren())
                .Where(i => i is Movie || i is Series || i is MusicAlbum || i is Game)
                .Select(i => i.OfficialRating)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => new Tuple<string, int?>(i, LocalizationManager.GetRatingLevel(i)))
                .OrderBy(i => i.Item2 ?? 1000)
                .Select(i => i.Item1);

            OfficialRating = ratings.FirstOrDefault() ?? currentOfficialRating;

            return !string.Equals(currentOfficialRating ?? string.Empty, OfficialRating ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var children = base.GetChildren(user, includeLinkedChildren);

            if (string.Equals(DisplayOrder, ItemSortBy.SortName, StringComparison.OrdinalIgnoreCase))
            {
                // Sort by name
                return LibraryManager.Sort(children, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
            }

            if (string.Equals(DisplayOrder, ItemSortBy.PremiereDate, StringComparison.OrdinalIgnoreCase))
            {
                // Sort by release date
                return LibraryManager.Sort(children, user, new[] { ItemSortBy.ProductionYear, ItemSortBy.PremiereDate, ItemSortBy.SortName }, SortOrder.Ascending);
            }

            // Default sorting
            return LibraryManager.Sort(children, user, new[] { ItemSortBy.ProductionYear, ItemSortBy.PremiereDate, ItemSortBy.SortName }, SortOrder.Ascending);
        }

        public BoxSetInfo GetLookupInfo()
        {
            return GetItemLookupInfo<BoxSetInfo>();
        }

        public override bool IsVisible(User user)
        {
            var userId = user.Id.ToString("N");

            // Need to check Count > 0 for boxsets created prior to the introduction of Shares
            if (Shares.Count > 0 && Shares.Any(i => string.Equals(userId, i.UserId, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (base.IsVisible(user))
            {
                return GetChildren(user, true).Any();
            }

            return false;
        }
    }
}
