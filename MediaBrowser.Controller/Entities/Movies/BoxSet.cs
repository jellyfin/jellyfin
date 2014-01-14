using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder, IHasTrailers, IHasTags, IHasKeywords, IHasPreferredMetadataLanguage, IHasDisplayOrder
    {
        public BoxSet()
        {
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            Tags = new List<string>();

            DisplayOrder = ItemSortBy.PremiereDate;
            Keywords = new List<string>();
        }

        public List<Guid> LocalTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }
        public List<string> Keywords { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>The display order.</value>
        public string DisplayOrder { get; set; }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedMovies;
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
    }
}
